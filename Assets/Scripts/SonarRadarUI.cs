using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SonarRadarUI : MonoBehaviour
{
    public static SonarRadarUI I { get; private set; }

    [Header("UI refs")]
    [Tooltip("Круглый фон радара (RectTransform).")]
    public RectTransform radarRect;

    [Tooltip("Префаб точки на радаре (Image/Graphic).")]
    public RectTransform dotPrefab;

    [Header("Настройки")]
    [Tooltip("Максимальная дистанция сканирования (мировые метры).")]
    public float worldRadius = 50f;

    List<GameObject> activeDots = new List<GameObject>();
    Coroutine hideRoutine;

    void Awake()
    {
        I = this;
        if (radarRect == null)
            radarRect = GetComponent<RectTransform>();

        ClearDots();

        // 🔹 Радар скрыт по умолчанию
        if (radarRect != null)
            radarRect.gameObject.SetActive(false);
    }

    /// <summary>
    /// Показать пинг: точки по направлениям на рыб относительно сабмарины.
    /// </summary>
    public void ShowPing(Transform submarine, List<FishSonarHighlight> fishes, float duration)
    {
        if (submarine == null || radarRect == null || dotPrefab == null)
            return;

        ClearDots();
        radarRect.gameObject.SetActive(true);

        float radiusPx = Mathf.Min(radarRect.rect.width, radarRect.rect.height) * 0.5f;

        foreach (var f in fishes)
        {
            if (f == null) continue;

            // направление и расстояние в мире
            Vector3 toFish = f.transform.position - submarine.position;
            toFish.y = 0f;

            float dist = toFish.magnitude;
            if (dist < 0.1f || dist > worldRadius) continue;

            Vector3 dirWorld = toFish.normalized;

            // переводим в локальные координаты субмарины,
            // чтобы "вверх" на радаре = нос лодки
            Vector3 dirLocal = Quaternion.Inverse(submarine.rotation) * dirWorld;
            Vector2 dir2 = new Vector2(dirLocal.x, dirLocal.z);
            if (dir2.sqrMagnitude < 0.0001f) continue;
            dir2.Normalize();

            // 0 в центре, worldRadius на краю круга
            float dist01 = Mathf.Clamp01(dist / worldRadius);
            float dotRadius = radiusPx * dist01;

            RectTransform dot = Instantiate(dotPrefab, radarRect);
            dot.anchoredPosition = dir2 * dotRadius;

            var graphic = dot.GetComponent<Graphic>();
            if (graphic != null)
                graphic.color = f.GetBaseColor();   // цвет по типу рыбы

            activeDots.Add(dot.gameObject);
        }

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideDotsAfter(duration));
    }


    IEnumerator HideDotsAfter(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        ClearDots();

        // 🔹 После окончания пинга полностью скрываем радар
        if (radarRect != null)
            radarRect.gameObject.SetActive(false);

        hideRoutine = null;
    }

    void ClearDots()
    {
        if (activeDots == null) return;
        foreach (var go in activeDots)
        {
            if (go != null) Destroy(go);
        }
        activeDots.Clear();
    }
}
