using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class FishSpawnZone : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ссылка на субмарину (игрок).")]
    public Transform submarine;

    [Tooltip("Какие рыбы могут спавниться в этой зоне.")]
    public GameObject[] fishPrefabs;

    [Header("Spawn rules")]
    [Tooltip("Задержка спавна после первого захода (сек).")]
    public float respawnDelayAfterFirstEnter = 30f;

    [Tooltip("В редакторе для теста можно форсировать поведение как у игрока.")]
    public bool debugAllowAnyCollider = false;

    [Header("Spawn area")]
    public Vector3 spawnAreaExtents = new Vector3(5f, 3f, 5f);
    public Vector3 spawnOffset = Vector3.zero;

    [Header("Fish movement params")]
    public float minSpeed = 1f;
    public float maxSpeed = 3f;
    public float fishLifetime = 20f;
    public float desiredDistanceFromSub = 8f;

    [Header("Randomization tweaks")]
    public Vector2 freqRandomMul = new Vector2(0.8f, 1.2f);
    public Vector2 ampRandomMul = new Vector2(0.8f, 1.2f);
    public Vector2 phaseRandomMul = new Vector2(0.8f, 1.2f);
    public Vector2 turnRandomMul = new Vector2(0.8f, 1.2f);

    private Collider triggerZone;

    // --- состояние ---
    private bool firstEnterDone = false;
    private GameObject currentFish;
    private Coroutine pendingSpawnCoroutine;

    void Awake()
    {
        triggerZone = GetComponent<Collider>();
        if (triggerZone != null)
            triggerZone.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        // Если рыба уже есть — ничего не делаем
        if (currentFish != null)
            return;

        // Если уже ждём отложенный спавн — тоже не дублируем
        if (pendingSpawnCoroutine != null)
            return;

        if (!firstEnterDone)
        {
            // 1-й заход — спавним сразу
            firstEnterDone = true;
            SpawnOneFish();
        }
        else
        {
            // последующие заходы — спавн через 30 секунд
            pendingSpawnCoroutine = StartCoroutine(SpawnAfterDelay(respawnDelayAfterFirstEnter));
        }
    }

    IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        pendingSpawnCoroutine = null;

        // Если за время ожидания рыба появилась (теоретически) — не спавним
        if (currentFish != null)
            yield break;

        SpawnOneFish();
    }

    void SpawnOneFish()
    {
        if (fishPrefabs == null || fishPrefabs.Length == 0)
            return;

        GameObject prefab = fishPrefabs[Random.Range(0, fishPrefabs.Length)];
        if (prefab == null)
            return;

        // случайная точка внутри объёма зоны
        Vector3 localRandom = new Vector3(
            Random.Range(-spawnAreaExtents.x, spawnAreaExtents.x),
            Random.Range(-spawnAreaExtents.y, spawnAreaExtents.y),
            Random.Range(-spawnAreaExtents.z, spawnAreaExtents.z)
        );

        Vector3 spawnPos = transform.position + spawnOffset + localRandom;

        Quaternion spawnRot;
        if (submarine != null)
        {
            Vector3 dir = submarine.forward;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            spawnRot = Quaternion.LookRotation(dir, Vector3.up);
        }
        else
        {
            spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        GameObject fishObj = Instantiate(prefab, spawnPos, spawnRot);
        currentFish = fishObj;

        // Привязываем "маячок", чтобы зона знала, когда рыба исчезла
        var marker = fishObj.GetComponent<FishSpawnedMarker>();
        if (marker == null)
            marker = fishObj.AddComponent<FishSpawnedMarker>();
        marker.ownerZone = this;

        // Настройка движения и жизни
        FishWiggle wiggle = fishObj.GetComponent<FishWiggle>();
        if (wiggle != null)
        {
            wiggle.submarine = submarine;
            wiggle.forwardSpeed = Random.Range(minSpeed, maxSpeed);
            wiggle.desiredDistance = desiredDistanceFromSub;
            wiggle.maxLifetime = fishLifetime;

            wiggle.frequency *= Random.Range(freqRandomMul.x, freqRandomMul.y);
            wiggle.amplitude *= Random.Range(ampRandomMul.x, ampRandomMul.y);
            wiggle.phaseOffset *= Random.Range(phaseRandomMul.x, phaseRandomMul.y);
            wiggle.turnSmooth *= Random.Range(turnRandomMul.x, turnRandomMul.y);
        }

        // сбрасываем caught для ловли
        FishData data = fishObj.GetComponent<FishData>();
        if (data != null)
            data.caught = false;
    }

    internal void NotifyFishDestroyed(GameObject fish)
    {
        if (currentFish == fish)
            currentFish = null;
    }

    bool IsPlayer(Collider other)
    {
        if (debugAllowAnyCollider) return true;

        return other.CompareTag("Player") ||
               other.GetComponent<SubmarineController>() != null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = Matrix4x4.TRS(transform.position + spawnOffset, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, spawnAreaExtents * 2f);
    }
#endif
}

/// <summary>
/// Служебный компонент на рыбе: сообщает зоне, когда рыба уничтожена.
/// </summary>
public class FishSpawnedMarker : MonoBehaviour
{
    [HideInInspector] public FishSpawnZone ownerZone;

    void OnDestroy()
    {
        if (ownerZone != null)
            ownerZone.NotifyFishDestroyed(gameObject);
    }
}
