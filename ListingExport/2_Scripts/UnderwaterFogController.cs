using UnityEngine;

public class UnderwaterFogController : MonoBehaviour
{
    [Header("Water Level (Y)")]
    public float waterSurfaceY = 0f; // высота поверхности воды в мире

    [Header("Fog density")]
    public float minFogDensity = 0.02f; // почти у поверхности
    public float maxFogDensity = 0.08f; // глубоко

    [Header("Fog colors")]
    public Color shallowColor = new Color(0.10f, 0.30f, 0.40f, 1f); // бирюзовый сверху
    public Color deepColor = new Color(0.02f, 0.08f, 0.12f, 1f); // тёмно-синий внизу

    [Header("Depth settings")]
    public float shallowDepth = 0.0f;   // прямо на поверхности
    public float deepDepth = 20.0f;  // что считаем "очень глубоко"

    void Update()
    {
        float camY = transform.position.y;

        // Насколько глубоко мы ниже поверхности воды?
        // camY выше воды -> depth = 0
        float depth = Mathf.Max(0f, waterSurfaceY - camY);

        // Нормализуем глубину в [0..1]
        float t = Mathf.InverseLerp(shallowDepth, deepDepth, depth);
        // сгладим чуть-чуть, чтобы не дёргалось
        t = Mathf.SmoothStep(0f, 1f, t);

        // Лерп плотности тумана
        float density = Mathf.Lerp(minFogDensity, maxFogDensity, t);
        RenderSettings.fogDensity = density;

        // Лерп цвета тумана (глубже = темнее/синее)
        Color fogCol = Color.Lerp(shallowColor, deepColor, t);
        RenderSettings.fogColor = fogCol;
    }
}
