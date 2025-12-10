using UnityEngine;
using System.Collections;

public class WorldBoundaryY : MonoBehaviour
{
    [Header("Ограничения по высоте (ось Y)")]
    public float minY = -50f;
    public bool useMinY = false;

    public float maxY = 15f;
    public bool useMaxY = true;

    public float boundaryDamage = 15f; // урон при выходе за границу

    [Header("Фейд экрана")]
    public CanvasGroup fadeCanvas;
    public float fadeDuration = 0.4f;

    [Header("Возврат в пределы")]
    public float pushForce = 5f;
    public float cooldownAfterReturn = 0.2f;

    Rigidbody rb;
    SubmarineController controller;
    SubmarineHealth health;
    SubmarineAudio audioRef; // ← ДОБАВИЛИ

    bool isRespawning = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<SubmarineController>();
        health = GetComponent<SubmarineHealth>();
        audioRef = GetComponent<SubmarineAudio>(); // ← аудио менеджер

        if (health == null)
            Debug.LogWarning("[WorldBoundaryY] SubmarineHealth не найден!");

        if (fadeCanvas == null)
            Debug.LogWarning("[WorldBoundaryY] fadeCanvas не назначен.");
    }

    void Update()
    {
        if (isRespawning) return;

        float y = transform.position.y;

        bool tooLow = useMinY && y < minY;
        bool tooHigh = useMaxY && y > maxY;

        if (tooLow || tooHigh)
        {
            StartCoroutine(ReturnInsideY());
        }
    }

    IEnumerator ReturnInsideY()
    {
        isRespawning = true;

        // отключить управление
        if (controller != null)
            controller.enabled = false;

        // остановить физику
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // нанести урон и звук аварии
        if (health != null && boundaryDamage > 0f)
        {
            health.ApplyDamage(boundaryDamage);
        }

        if (audioRef != null)
        {
            audioRef.PlayBoundaryHit();
        }

        // затемнение
        yield return StartCoroutine(FadeTo(1f));

        // вернуть в зону
        Vector3 p = transform.position;
        if (useMinY && p.y < minY) p.y = minY;
        if (useMaxY && p.y > maxY) p.y = maxY;
        transform.position = p;

        // подождать кадр
        yield return null;

        // включить физику обратно (слегка толкнуть вниз-вперёд)
        if (rb != null)
        {
            rb.isKinematic = false;
            Vector3 shoveDir = (Vector3.down - transform.forward * 0.5f).normalized;
            rb.velocity = shoveDir * pushForce;
            rb.angularVelocity = Vector3.zero;
        }

        // убрать затемнение
        yield return StartCoroutine(FadeTo(0f));

        // вернуть управление игроку
        if (controller != null)
            controller.enabled = true;

        // короткий кулдаун
        yield return new WaitForSeconds(cooldownAfterReturn);

        isRespawning = false;
    }

    IEnumerator FadeTo(float targetAlpha)
    {
        if (fadeCanvas == null)
            yield break;

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
