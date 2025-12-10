using System;
using System.Collections;
using UnityEngine;

public enum HarvestMode { Fillet, Carcass }

public class ButcherSequencer : MonoBehaviour
{
    [Header("Fish root (визуальная рыба)")]
    [Tooltip("Корень, под которым лежат все части рыбы. Если не указать, будет this.transform.")]
    public Transform fishRoot;

    [Header("Parts (assign in prefab)")]
    public GameObject head;           // голова
    public GameObject[] fins;         // плавники
    public GameObject tail;           // хвост
    public GameObject skin;           // тело/кожа (Fish_Body)
    public GameObject bones;          // отдельный видимый меш костей (НЕ Armature!)

    [Header("Innards (fillet result)")]
    [Tooltip("Внутренности/филе. При режиме Филе остаются на месте, остальное отлетает.")]
    public GameObject innards;

    [Header("Debris visual (отрезанные части)")]
    [Tooltip("Куда складывать отлетевшие куски. Если пусто — в корень сцены.")]
    public Transform debrisRoot;

    [Tooltip("Сколько секунд обрезки лежат, после чего удаляются. 0 или меньше — не удалять и событие AllDebrisCleared не сработает.")]
    public float debrisLifetime = 15f;

    [Tooltip("Сила стартового толчка вниз (VelocityChange).")]
    public float dropExtraForce = 3f;

    [Tooltip("Случайный крутящий момент при падении.")]
    public bool randomRotationOnDrop = true;

    [Header("Debris physics")]
    [Tooltip("Через сколько секунд включать/создавать коллайдеры у обломков.")]
    public float debrisColliderDelay = 0.35f;

    [Tooltip("На baked-обломок добавлять MeshCollider (convex). Если false — BoxCollider по bounds меша.")]
    public bool useMeshColliderForDebris = true;

    public PhysicMaterial debrisPhysicMaterial;

    [Tooltip("Слой для обломков. -1 — не менять.")]
    public int debrisLayer = -1;

    [Header("Timing (seconds)")]
    public float stepDelay = 0.25f;
    public float endDelay = 0.3f;

    [Header("Debug")]
    public bool debugLogs = true; // сейчас не используется, можно удалить

    bool running;
    HarvestMode currentMode;

    // Контекст от HarvestUIManager
    [NonSerialized] public FishCatcher catcher;
    [NonSerialized] public FishData fish;
    [NonSerialized] public GameObject fishObj; // инстанс рыбы в мире

    // ==== событие, когда все обломки уничтожены ====
    public event Action AllDebrisCleared;
    int _activeDebris = 0;

    void Awake()
    {
        if (!fishRoot) fishRoot = transform;

        if (innards) innards.SetActive(false);
        // ВАЖНО: НЕ выключать Armature. Поле 'bones' должно ссылаться на отдельный визуальный меш костей (если он есть).
        // if (bones) bones.SetActive(false); // НЕ ДЕЛАТЬ
    }

    /// <summary>Вызвать перед StartButchering, чтобы секвенсер знал, кого разделывает.</summary>
    public void Setup(FishCatcher fc, FishData data, GameObject obj)
    {
        catcher = fc;
        fish = data;
        fishObj = obj;
    }

    // === PUBLIC API =======================================================

    public void StartButchering(HarvestMode mode, Action onDone)
    {
        if (running) return;

        currentMode = mode;

        // ТОЛЬКО выключаем коллайдеры, рендеры не трогаем
        DisableAllCollidersUnderFish();

        running = true;
        StartCoroutine(mode == HarvestMode.Carcass
            ? FlowCarcass(onDone)
            : FlowFillet(onDone));
    }

    IEnumerator FlowCarcass(Action onDone)
    {
        // ТУША: отрубаем голову, тело остаётся (но без коллайдеров)
        CutHead();
        yield return new WaitForSeconds(stepDelay);

        yield return new WaitForSeconds(endDelay);

        running = false;
        onDone?.Invoke();
    }

    IEnumerator FlowFillet(Action onDone)
    {
        // ФИЛЕ: по шагам отрубаем всё, филе/innards остаётся
        CutHead(); yield return new WaitForSeconds(stepDelay);
        CutFins(); yield return new WaitForSeconds(stepDelay);
        CutTail(); yield return new WaitForSeconds(stepDelay);
        RemoveSkin(); yield return new WaitForSeconds(stepDelay);
        ExtractBones(); yield return new WaitForSeconds(endDelay);

        running = false;
        onDone?.Invoke();
    }

    // === STEPS ============================================================

    void CutHead()
    {
        // Кровь/FX в момент отрубания головы
        if (catcher != null && fish != null && fishObj != null)
            catcher.SpawnButcherFX(fish, fishObj);

        DropPart(head);
        head = null;
    }

    void CutFins()
    {
        if (fins != null)
        {
            for (int i = 0; i < fins.Length; i++)
            {
                if (!fins[i]) continue;
                DropPart(fins[i]);
                fins[i] = null;
            }
        }
    }

    void CutTail()
    {
        DropPart(tail);
        tail = null;
    }

    void RemoveSkin()
    {
        // Для ФИЛЕ: кожа/тело падает как обломок, innards остаются на месте
        if (currentMode == HarvestMode.Fillet)
        {
            Transform p = skin ? skin.transform.parent : null;
            Vector3 lp = skin ? skin.transform.localPosition : Vector3.zero;
            Quaternion lr = skin ? skin.transform.localRotation : Quaternion.identity;
            Vector3 ls = skin ? skin.transform.localScale : Vector3.one;

            DropPart(skin);
            skin = null;

            if (innards)
            {
                var t = innards.transform;
                if (p && t.parent != p) t.SetParent(p, false);
                t.localPosition = lp;
                t.localRotation = lr;
                t.localScale = ls;
                innards.SetActive(true);
            }
        }
        else
        {
            // Carcass: туша остаётся, ничего не делаем с skin
        }
    }

    void ExtractBones()
    {
        // В режиме Филе — кости падают как обломок (если есть отдельный визуал костей)
        if (currentMode == HarvestMode.Fillet && bones)
        {
            if (!bones.activeSelf) bones.SetActive(true);
            DropPart(bones);
            bones = null;
        }
    }

    // === COLLIDERS ========================================================

    /// <summary>Полностью отключает все коллайдеры рыбы (чтобы она больше не била сабмарину).</summary>
    void DisableAllCollidersUnderFish()
    {
        Transform root = null;
        if (fishRoot) root = fishRoot;
        else if (fishObj) root = fishObj.transform;
        else root = transform;

        var cols = root.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
            c.enabled = false;
    }

    // === DEBRIS MANAGEMENT ================================================

    void RegisterDebris(GameObject g)
    {
        _activeDebris++;
        if (debrisLayer >= 0) g.layer = debrisLayer;
    }

    void DebrisDestroyed()
    {
        _activeDebris = Mathf.Max(0, _activeDebris - 1);
        if (_activeDebris == 0)
            AllDebrisCleared?.Invoke();
    }

    IEnumerator ArmDebrisColliders(GameObject debris)
    {
        if (!debris) yield break;

        float t = debrisColliderDelay;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime; // не зависит от Time.timeScale
            yield return null;
        }

        // Включаем или создаём коллайдеры
        var cols = debris.GetComponentsInChildren<Collider>(true);
        if (cols == null || cols.Length == 0)
        {
            var mf = debris.GetComponent<MeshFilter>();
            if (useMeshColliderForDebris && mf && mf.sharedMesh)
            {
                var mc = debris.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = true;
                if (debrisPhysicMaterial) mc.sharedMaterial = debrisPhysicMaterial;
            }
            else
            {
                var bc = debris.AddComponent<BoxCollider>();
                if (mf && mf.sharedMesh)
                {
                    var b = mf.sharedMesh.bounds;
                    bc.center = b.center;
                    bc.size = b.size;
                }
                if (debrisPhysicMaterial) bc.sharedMaterial = debrisPhysicMaterial;
            }
        }
        else
        {
            foreach (var c in cols) c.enabled = true;
        }
    }

    // === DEBRIS VISUAL DROP ===============================================

    // Запекаем skinned-меш в отдельный GO и роняем его
    GameObject BakeSkinnedToDebris(GameObject src)
    {
        if (!src) return null;

        var smr = src.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (!smr) return null;

        var debris = new GameObject(src.name + "_Debris");
        debris.transform.position = smr.transform.position;
        debris.transform.rotation = smr.transform.rotation;
        debris.transform.localScale = smr.transform.lossyScale;

        var mf = debris.AddComponent<MeshFilter>();
        var mr = debris.AddComponent<MeshRenderer>();

        var baked = new Mesh();
        smr.BakeMesh(baked, true);
        mf.sharedMesh = baked;
        mr.sharedMaterials = smr.sharedMaterials;

        if (debrisRoot) debris.transform.SetParent(debrisRoot, true);

        var rb = debris.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (dropExtraForce > 0f)
        {
            rb.AddForce(Vector3.down * dropExtraForce, ForceMode.VelocityChange);
            if (randomRotationOnDrop)
                rb.AddTorque(UnityEngine.Random.onUnitSphere * dropExtraForce, ForceMode.VelocityChange);
        }

        smr.enabled = false;

        RegisterDebris(debris);
        StartCoroutine(ArmDebrisColliders(debris));

        if (debrisLifetime > 0f)
            StartCoroutine(DestroyDebrisAfter(debris, debrisLifetime));

        return debris;
    }

    void DropPart(GameObject part)
    {
        if (!part)
        {
            return;
        }

        var baked = BakeSkinnedToDebris(part);
        if (baked) return;

        Transform t = part.transform;

        if (debrisRoot != null)
            t.SetParent(debrisRoot, true);
        else
            t.SetParent(null, true);

        var partColliders = part.GetComponentsInChildren<Collider>(true);
        foreach (var c in partColliders) c.enabled = false;

        Rigidbody rb = part.GetComponent<Rigidbody>();
        if (!rb) rb = part.AddComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.None;
        rb.isKinematic = false;
        rb.useGravity = true;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (dropExtraForce > 0f)
        {
            rb.AddForce(Vector3.down * dropExtraForce, ForceMode.VelocityChange);

            if (randomRotationOnDrop)
                rb.AddTorque(UnityEngine.Random.onUnitSphere * dropExtraForce, ForceMode.VelocityChange);
        }

        RegisterDebris(part);
        StartCoroutine(ArmDebrisColliders(part));

        if (debrisLifetime > 0f)
            StartCoroutine(DestroyDebrisAfter(part, debrisLifetime));
    }

    IEnumerator DestroyDebrisAfter(GameObject obj, float lifetime)
    {
        float elapsed = 0f;
        while (obj && elapsed < lifetime)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (obj)
            Destroy(obj);

        DebrisDestroyed();
    }
}
