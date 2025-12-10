using UnityEngine;
using System.Collections;

public class WorldLimitZone : MonoBehaviour
{
    [Header("Куда возвращать игрока")]
    public Transform respawnPoint;        // точка внутри лабиринта

    [Header("Затемнение экрана")]
    public CanvasGroup fadeCanvas;        // CanvasGroup на полноэкранной тёмной картинке
    public float fadeDuration = 0.4f;

    [Header("После респауна")]
    public float pushForce = 5f;

    [Header("Отладка")]
    public bool logDebug = true;

    bool isRespawning = false;

    void Awake()
    {
        if (logDebug) Debug.Log("[WorldLimitZone] Awake на объекте: " + gameObject.name);

        // Быстрая проверка конфигурации прямо в рантайме:
        if (!GetComponent<Collider>())
        {
            Debug.LogError("[WorldLimitZone] НЕТ Collider на " + name + " (должен быть BoxCollider)");
        }
        else
        {
            var col = GetComponent<Collider>();
            if (!col.isTrigger)
            {
                Debug.LogError("[WorldLimitZone] Collider не помечен как Trigger на " + name + " (isTrigger должен быть true)");
            }
        }

        if (respawnPoint == null)
        {
            Debug.LogWarning("[WorldLimitZone] respawnPoint пустой. Телепорт будет пропущен.");
        }
        if (fadeCanvas == null)
        {
            Debug.LogWarning("[WorldLimitZone] fadeCanvas пустой. Затемнение не покажется.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (logDebug) Debug.Log("[WorldLimitZone] OnTriggerEnter с " + other.name + " (tag=" + other.tag + ")");

        // мы не проверяем тег жёстко, мы просто обрабатываем если есть Rigidbody
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && !isRespawning)
        {
            StartCoroutine(HandleRespawn(other, rb));
        }
    }

    IEnumerator HandleRespawn(Collider other, Rigidbody rb)
    {
        isRespawning = true;

        if (logDebug) Debug.Log("[WorldLimitZone] HANDLE RESPAWN старт для " + other.name);

        // 0. Отключаем контроллер лодки, чтобы она не сопротивлялась
        var controller = other.GetComponent<SubmarineController>();
        if (controller != null)
        {
            controller.enabled = false;
            if (logDebug) Debug.Log("[WorldLimitZone] Отключил SubmarineController на " + other.name);
        }

        // 1. Замораживаем физику на время телепорта
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // 2. Фейд ТЁМНО
        yield return StartCoroutine(FadeTo(1f));

        // 3. Телепорт внутрь
        if (respawnPoint != null)
        {
            other.transform.position = respawnPoint.position;
            other.transform.rotation = respawnPoint.rotation;

            if (logDebug) Debug.Log("[WorldLimitZone] Телепортнул в " + respawnPoint.position);
        }
        else
        {
            if (logDebug) Debug.LogWarning("[WorldLimitZone] respawnPoint не назначен, некуда телепортировать.");
        }

        // 4. Ждём кадр, потом возвращаем физику
        yield return null;
        rb.isKinematic = false;
        rb.velocity = other.transform.forward * pushForce;
        rb.angularVelocity = Vector3.zero;

        // 5. Фейд ОБРАТНО
        yield return StartCoroutine(FadeTo(0f));

        // 6. Включаем управление обратно
        if (controller != null)
        {
            controller.enabled = true;
            if (logDebug) Debug.Log("[WorldLimitZone] Включил SubmarineController обратно");
        }

        isRespawning = false;
        if (logDebug) Debug.Log("[WorldLimitZone] HANDLE RESPAWN конец");
    }

    IEnumerator FadeTo(float targetAlpha)
    {
        if (fadeCanvas == null)
        {
            // если не задан — просто скипаем
            yield break;
        }

        float startAlpha = fadeCanvas.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = t / fadeDuration;
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, k);
            yield return null;
        }

        fadeCanvas.alpha = targetAlpha;
    }
}
