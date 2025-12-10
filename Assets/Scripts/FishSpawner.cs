using UnityEngine;
using System.Collections.Generic;

public class FishSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform submarine;           // субмарина
    public GameObject[] fishPrefabs;      // префабы рыб (каждая с FishWiggle + FishData)

    [Header("Spawn logic")]
    [Tooltip("Каждые N метров пути лодки делаем попытку спавна.")]
    public float distanceBetweenSpawns = 20f;

    [Tooltip("Максимум живых рыб, чтобы не засорять сцену.")]
    public int maxFishAlive = 30;

    [Header("Where to spawn (базовые хотелки)")]
    [Tooltip("Базовое расстояние ВПЕРЁД по курсу субмарины, куда целимся спавном.")]
    public float aheadDistance = 15f;

    [Tooltip("Разброс влево/вправо от курса (по локальной оси субмарины).")]
    public float sideScatter = 8f;

    [Tooltip("Разброс вверх/вниз от курса (по локальной оси субмарины).")]
    public float upDownScatter = 4f;

    [Header("Corridor safety")]
    [Tooltip("Какие слои считаются препятствием (скалы, стены лабиринта).")]
    public LayerMask obstacleMask;

    [Tooltip("Минимальная дистанция свободного пространства вперёд, чтобы вообще пытаться спавнить.")]
    public float minFreeAhead = 2f;

    [Tooltip("Насколько отступать от стены, чтобы не родить рыбу прям внутри коллайдера.")]
    public float wallBuffer = 1f;

    [Header("Fish params")]
    [Tooltip("Мин. скорость рыбы (FishWiggle.forwardSpeed).")]
    public float minSpeed = 1f;

    [Tooltip("Макс. скорость рыбы (FishWiggle.forwardSpeed).")]
    public float maxSpeed = 3f;

    [Tooltip("Сколько живёт рыба до автоудаления (FishWiggle.maxLifetime).")]
    public float fishLifetime = 20f;

    [Tooltip("На каком расстоянии от подлодки рыба старается держаться.")]
    public float desiredDistanceFromSub = 8f;

    private Vector3 lastSpawnRefPos;
    private bool initialized = false;
    private List<GameObject> alive = new List<GameObject>();

    void Update()
    {
        if (submarine == null || fishPrefabs == null || fishPrefabs.Length == 0)
            return;

        // чистим null'ы (рыб, которые уже уничтожены)
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            if (alive[i] == null)
                alive.RemoveAt(i);
        }

        // не спавним, если мы уже забиты
        if (alive.Count >= maxFishAlive)
            return;

        if (!initialized)
        {
            initialized = true;
            lastSpawnRefPos = submarine.position;
            return;
        }

        float traveled = Vector3.Distance(submarine.position, lastSpawnRefPos);

        if (traveled >= distanceBetweenSpawns)
        {
            lastSpawnRefPos = submarine.position;
            TrySpawnFish();
        }
    }

    void TrySpawnFish()
    {
        // считаем безопасную позицию спавна в лабиринте
        Vector3 spawnPos;
        Quaternion spawnRot;

        if (!ComputeSafeSpawn(out spawnPos, out spawnRot))
        {
            // узкий коридор / сразу стена — не спавним
            return;
        }

        // выберем случайный префаб
        GameObject prefab = fishPrefabs[Random.Range(0, fishPrefabs.Length)];
        if (!prefab) return;

        // создаём рыбу
        GameObject fish = Instantiate(prefab, spawnPos, spawnRot);

        // настроим FishWiggle
        FishWiggle wiggle = fish.GetComponent<FishWiggle>();
        if (wiggle != null)
        {
            wiggle.submarine = submarine;
            wiggle.forwardSpeed = Random.Range(minSpeed, maxSpeed);
            wiggle.desiredDistance = desiredDistanceFromSub;
            wiggle.maxLifetime = fishLifetime;

            // немного разнообразия анимации
            wiggle.frequency *= Random.Range(0.8f, 1.2f);
            wiggle.amplitude *= Random.Range(0.8f, 1.2f);
            wiggle.phaseOffset *= Random.Range(0.8f, 1.2f);
            wiggle.turnSmooth *= Random.Range(0.8f, 1.2f);
        }

        alive.Add(fish);

        // можно включить лог, чтоб понимать где рыба реально спавнится
        // Debug.Log("[FishSpawner] Spawned " + fish.name + " at " + spawnPos);
    }

    bool ComputeSafeSpawn(out Vector3 safePos, out Quaternion safeRot)
    {
        // по умолчанию
        safePos = submarine.position;
        safeRot = Quaternion.identity;

        Vector3 forward = submarine.forward.normalized;
        Vector3 right = submarine.right;
        Vector3 up = submarine.up;

        // 1. проверяем свободное пространство впереди субмарины
        //    рейкастим вперёд до aheadDistance по obstacleMask
        float freeAhead = aheadDistance;
        RaycastHit hit;

        if (Physics.Raycast(
                submarine.position,
                forward,
                out hit,
                aheadDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
        {
            // стена / скала нашлась раньше, чем aheadDistance
            // значит нельзя спавнить за стеной, отступаем немного до неё
            freeAhead = hit.distance - wallBuffer;
        }

        // если прямо перед нами стена вплотную → не спавним сейчас
        if (freeAhead < minFreeAhead)
        {
            return false;
        }

        // 2. боковой и вертикальный разброс
        //    в тесном коридоре (freeAhead маленький) нам нельзя слишком разбрасывать в стороны,
        //    иначе мы снова уйдём в стену. Поэтому адаптивно уменьшаем разброс.
        //
        // идея простая:
        //  - если у нас много свободного вперёд (открытая комната), оставляем полный scatter
        //  - если коридор короткий, режем scatter в 2-3 раза
        //
        float corridorTightness = Mathf.Clamp01(freeAhead / aheadDistance);
        // corridorTightness = 1 → простор, 0 → супер тесно
        // хотим наоборот: чем теснее, тем меньше разброс
        float scatterScale = Mathf.Lerp(0.3f, 1f, corridorTightness);
        float side = Random.Range(-sideScatter, sideScatter) * scatterScale;
        float vertical = Random.Range(-upDownScatter, upDownScatter) * scatterScale;

        // 3. вычисляем итоговую позицию
        safePos =
            submarine.position
            + forward * freeAhead
            + right * side
            + up * vertical;

        // 4. направление рыбы — пусть смотрит примерно куда плывёт субмарина,
        //    чтобы визуально они не стояли боком
        safeRot = Quaternion.LookRotation(forward, Vector3.up);

        return true;
    }
}
