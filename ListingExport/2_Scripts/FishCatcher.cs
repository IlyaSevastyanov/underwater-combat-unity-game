using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class FishCatcher : MonoBehaviour
{
    public enum FlowDirection { SubForward, ToCamera }

    // Локальная ось, вдоль которой стреляет твой FX-префаб
    public enum EmitterAxis { ZForward, YUp, XRight }

    [Header("Refs")]
    public SubmarineHealth subHealth;
    private ScoreManager scoreManager;
    public SubmarineController subController;

    [Header("Logic")]
    public HarvestUIManager modalLogic_PC;           // ПК версия
    public HarvestUIManagerMobile modalLogic_Mobile; // Мобильная версия

    [Header("Mobile")]
    public bool forceMobileInEditor = false;

    [Header("Damage feedback / Invuln")]
    public SubmarineInvulnerabilityBlink subInvuln;

    [Header("Fish filter (by tag)")]
    [Tooltip("Тег объектов рыбы, которые можно ловить")]
    public string fishTag = "Fish";

    public static event System.Action OnAnyFishCaught;
    public static event System.Action<bool> OnAnyFishResolved;

    [Header("Audio")]
    [Tooltip("Сюда кинь объект Submarine (на котором висит SubmarineAudio).")]
    public SubmarineAudio audioRef;

    [Header("Catch behaviour")]
    [Tooltip("Задержка перед тем, как снова можно ловить следующую рыбу.")]
    public float recatchDelay = 0.5f;

    [Header("Direction Controls")]
    [Tooltip("Игнорировать высоту укуса и стрелять строго вперёд по сабмарине")]
    public bool alwaysForward = true;

    [Tooltip("Проецировать направление на горизонтальную плоскость (убрать Y-компонент)")]
    public bool flattenVertical = true;

    [Tooltip("Если вертикаль не полностью вырубать: максимум наклона по питчу, градусы")]
    [Range(0f, 89f)] public float maxPitchDegrees = 10f;

    [Header("FX Spawn Point")]
    [Tooltip("Где спавнить кровь/брызги. Пустой объект у носа субмарины, ЧУТЬ впереди камеры.")]
    public Transform fxSpawnPoint;

    [Header("FX Direction (без правки модулей)")]
    [Tooltip("Куда направлять поток партиклов (только поворот, без изменения модулей).")]
    public FlowDirection flowDirection = FlowDirection.SubForward;

    [Tooltip("Доп. поворот в градусах, если эмиттер у тебя смотрит вбок/назад. Например (0,90,0).")]
    public Vector3 fxRotationOffsetEuler = Vector3.zero;

    [Header("FX Axis Mapping")]
    [Tooltip("Какой локальной осью префаб стреляет. Выбери, чтобы переориентировать её в +Z.")]
    public EmitterAxis emitterLocalAxis = EmitterAxis.YUp;

    [Header("FX Defaults (подстраховка)")]
    [Tooltip("Если у конкретной рыбы не задан butcherFXPrefab — возьмём этот.")]
    public ParticleSystem defaultButcherFX;

    [Header("FX Lifetime")]
    [Tooltip("Если > 0, FX будет удалён через это время (сек). Если 0 или меньше — время вычисляется по ParticleSystem.")]
    public float fxLifetimeOverride = 0f;

    Collider catchZoneCollider;

    HarvestUIManager harvestUI;   // итоговый выбранный менеджер (пока не используется)
    bool isMobileLike;

    void Awake()
    {
        catchZoneCollider = GetComponent<Collider>();
        if (catchZoneCollider != null) catchZoneCollider.isTrigger = true;

        // --- определяем, мобильный UX или нет ---
        bool isMobileRuntime = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: смотрим, мобильный ли браузер через JS-плагин
        isMobileRuntime = WebGLBrowserCheck.IsMobileBrowser();
#elif UNITY_ANDROID || UNITY_IOS
        // Нативные мобильные платформы
        isMobileRuntime = true;
#else
        // Остальное — десктоп
        isMobileRuntime = false;
#endif

        // мобильный UX:
        //  - на Android/iOS/мобильном WebGL
        //  - или в редакторе, если включён флаг forceMobileInEditor
        isMobileLike = isMobileRuntime || (Application.isEditor && forceMobileInEditor);

        if (!scoreManager)
            scoreManager = FindObjectOfType<ScoreManager>();

        Debug.Log($"[FishCatcher] platform={Application.platform}, " +
                  $"isMobileRuntime={isMobileRuntime}, " +
                  $"forceMobileInEditor={forceMobileInEditor}, " +
                  $"isMobileLike={isMobileLike}");
    }

    void OnTriggerEnter(Collider other)
    {
        // 0) Если лодка сейчас в инвулне — вообще не ловим рыбу
        if (subInvuln != null && subInvuln.IsInvulnerable)
            return;

        // 1) Проверка по ТЕГУ
        bool isFishByTag =
            other.CompareTag(fishTag) ||
            (other.transform.root != null && other.transform.root.CompareTag(fishTag));

        if (!isFishByTag)
            return;

        // 2) Берём FishData у родителя
        FishData fish = other.GetComponentInParent<FishData>();
        if (fish == null || fish.caught)
            return;

        // помечаем, что рыба уже поймана
        fish.caught = true;
        OnAnyFishCaught?.Invoke();

        // отключаем триггер и управление сабмариной на время UI
        if (catchZoneCollider != null) catchZoneCollider.enabled = false;
        if (subController != null) subController.enabled = false;

        // ПК / мобильный UI
        if (isMobileLike)
        {
            if (modalLogic_Mobile != null)
                modalLogic_Mobile.ShowPrompt(fish, fish.gameObject, this);
        }
        else
        {
            if (modalLogic_PC != null)
                modalLogic_PC.ShowPrompt(fish, fish.gameObject, this);
        }
    }

    public void ResolveCatch(FishData fish, GameObject fishObj, bool butcher)
    {
        if (subController != null) subController.enabled = true;
        StartCoroutine(ReenableCatchZoneAfterDelay());

        // Игрок выбрал "отпустить"
        if (!butcher)
        {
            if (fishObj != null) Destroy(fishObj);

            OnAnyFishResolved?.Invoke(false);
            return;
        }

        // --- ЯДОВИТАЯ РЫБА: включаем инвулн ---
        if (fish != null && fish.istoxic)
        {
            if (subInvuln != null)
                subInvuln.TriggerInvulnerability();
        }

        // ⚠️ FX крови запускается в ButcherSequencer, тут не трогаем

        if (fish != null && fish.isHostile)
        {
            if (scoreManager != null) scoreManager.AddScore(-fish.scorePenalty);
            if (subHealth != null) subHealth.ApplyDamage(fish.healthPenalty);
            if (audioRef != null) audioRef.PlayHostileFish();
        }
        else if (fish != null)
        {
            if (scoreManager != null) scoreManager.AddScore(fish.scoreValue);
            if (subHealth != null && fish.healthHeal > 0f)
                subHealth.currentHealth = Mathf.Min(subHealth.maxHealth, subHealth.currentHealth + fish.healthHeal);
            if (audioRef != null) audioRef.PlayScoreGain();
        }

        OnAnyFishResolved?.Invoke(true);

        if (fishObj != null) Destroy(fishObj);
    }

    public void SpawnButcherFX(FishData fish, GameObject fishObj)
    {
        // 0) префаб FX: у рыбы или дефолтный
        ParticleSystem fxPrefab =
            (fish != null && fish.butcherFXPrefab != null) ? fish.butcherFXPrefab : defaultButcherFX;

        if (fxPrefab == null)
        {
            return;
        }

        // 1) позиция спавна
        Vector3 mouthPos = fxSpawnPoint ? fxSpawnPoint.position : transform.position;
        Vector3 spawnPos = mouthPos;
        if (fishObj != null)
        {
            var col = fishObj.GetComponentInChildren<Collider>();
            spawnPos = col ? col.ClosestPoint(mouthPos) : fishObj.transform.position;
        }

        // 2) направление струи
        Vector3 dir;

        if (flowDirection == FlowDirection.ToCamera && Camera.main != null)
        {
            dir = Camera.main.transform.forward;
        }
        else
        {
            if (alwaysForward)
                dir = (subController ? subController.transform.forward : transform.forward);
            else
                dir = (spawnPos - mouthPos);

            if (dir.sqrMagnitude < 1e-6f)
                dir = (subController ? subController.transform.forward : transform.forward);

            if (flattenVertical)
            {
                dir = Vector3.ProjectOnPlane(dir, Vector3.up);
            }
            else if (maxPitchDegrees < 89f)
            {
                Vector3 flat = Vector3.ProjectOnPlane(dir, Vector3.up);
                if (flat.sqrMagnitude > 1e-6f)
                {
                    flat.Normalize();
                    float pitch = Vector3.Angle(flat, dir.normalized); // 0..90
                    if (pitch > maxPitchDegrees)
                    {
                        float t = Mathf.Clamp01(maxPitchDegrees / Mathf.Max(pitch, 1e-4f));
                        dir = Vector3.Slerp(flat, dir.normalized, t);
                    }
                }
            }

            dir.Normalize();
        }

        // 3) поворот с маппингом локальной оси эмиттера в +Z
        Quaternion aim = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion axisFix = Quaternion.identity;
        switch (emitterLocalAxis)
        {
            default:
            case EmitterAxis.ZForward: axisFix = Quaternion.identity; break;
            case EmitterAxis.YUp: axisFix = Quaternion.FromToRotation(Vector3.up, Vector3.forward); break;
            case EmitterAxis.XRight: axisFix = Quaternion.FromToRotation(Vector3.right, Vector3.forward); break;
        }
        Quaternion rot = aim * axisFix;
        if (fxRotationOffsetEuler != Vector3.zero) rot *= Quaternion.Euler(fxRotationOffsetEuler);

        // 4) инстанс FX (КОРНЕВАЯ система)
        ParticleSystem rootPs = Instantiate(fxPrefab, spawnPos, rot);

        // 5) запускаем ВСЕ ParticleSystem под корнем
        ParticleSystem[] systems = rootPs.GetComponentsInChildren<ParticleSystem>(true);
        float maxLife = 0f;

        foreach (var ps in systems)
        {
            if (!ps) continue;

            ps.Play(true);

            var main = ps.main;
            float duration = main.duration;

            float lifetimeExtra = 0f;
            switch (main.startLifetime.mode)
            {
                case ParticleSystemCurveMode.TwoConstants:
                    lifetimeExtra = Mathf.Max(main.startLifetime.constantMin, main.startLifetime.constantMax);
                    break;
                case ParticleSystemCurveMode.TwoCurves:
                case ParticleSystemCurveMode.Curve:
                    lifetimeExtra = main.startLifetime.constantMax;
                    break;
                case ParticleSystemCurveMode.Constant:
                    lifetimeExtra = main.startLifetime.constant;
                    break;
            }

            float total = duration + lifetimeExtra;
            if (main.loop) total = Mathf.Max(total, 5f);

            if (total > maxLife) maxLife = total;
        }

        float destroyTime = fxLifetimeOverride > 0f ? fxLifetimeOverride : maxLife;
        if (destroyTime <= 0f) destroyTime = 5f;

        Destroy(rootPs.gameObject, destroyTime);
    }

    private IEnumerator ReenableCatchZoneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(recatchDelay);
        if (catchZoneCollider != null) catchZoneCollider.enabled = true;
    }
}
