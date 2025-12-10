using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishSonarHighlight : MonoBehaviour
{
    [Header("Цвета по типу рыбы")]
    public Color edibleColor = Color.cyan;       // съедобная
    public Color hostileColor = Color.yellow;    // агрессивная
    public Color toxicColor = Color.red;         // ядовитая

    [Header("Параметры подсветки")]
    public float defaultDuration = 2f;
    public float emissionIntensity = 2f;         // во сколько раз усиливать эмиссию

    // запись для одного материала
    private class MatEntry
    {
        public Renderer renderer;
        public int materialIndex;
        public string colorProperty;      // "_Color" или "_BaseColor"
        public Color originalColor;

        public bool hasEmission;
        public Color originalEmissionColor;
    }

    List<MatEntry> matEntries = new List<MatEntry>();
    Coroutine currentRoutine;

    FishData fishData;

    void Awake()
    {
        fishData = GetComponentInParent<FishData>();

        matEntries.Clear();

        // Берём все рендереры рыбы (включая детей, скины, плавники и т.п.)
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (var rend in renderers)
        {
            if (rend == null) continue;

            var mats = rend.materials; // экземпляры материалов, не shared
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat == null) continue;

                // --- базовый цвет ---
                string prop = null;
                if (mat.HasProperty("_Color"))
                    prop = "_Color";
                else if (mat.HasProperty("_BaseColor"))
                    prop = "_BaseColor";

                if (prop == null)
                    continue; // материал без обычного цвета — пропускаем

                var entry = new MatEntry
                {
                    renderer = rend,
                    materialIndex = i,
                    colorProperty = prop,
                    originalColor = mat.GetColor(prop),
                    hasEmission = false,
                    originalEmissionColor = Color.black
                };

                // --- эмиссия (если есть) ---
                if (mat.HasProperty("_EmissionColor"))
                {
                    entry.hasEmission = true;
                    entry.originalEmissionColor = mat.GetColor("_EmissionColor");
                }

                matEntries.Add(entry);
            }
        }
    }

    /// <summary>Базовый цвет подсветки для этой рыбы (по типу).</summary>
    public Color GetBaseColor()
    {
        if (fishData != null)
        {
            if (fishData.istoxic)  // поле у тебя уже есть
                return toxicColor;

            if (fishData.isHostile)
                return hostileColor;
        }

        return edibleColor;
    }

    public void Ping(float duration)
    {
        if (duration <= 0f) duration = defaultDuration;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(PingRoutine(duration));
    }

    IEnumerator PingRoutine(float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float t01 = Mathf.Clamp01(t / duration);

            // гладкий пульс: 0 → 1 → 0
            float pulse = Mathf.Sin(t01 * Mathf.PI);
            pulse = Mathf.Clamp01(pulse);
            pulse = Mathf.SmoothStep(0f, 1f, pulse);

            Color targetColor = GetBaseColor();

            foreach (var entry in matEntries)
            {
                if (entry.renderer == null) continue;

                var mats = entry.renderer.materials;
                if (entry.materialIndex < 0 || entry.materialIndex >= mats.Length) continue;

                var mat = mats[entry.materialIndex];
                if (mat == null || !mat.HasProperty(entry.colorProperty)) continue;

                // цвет
                Color c = Color.Lerp(entry.originalColor, targetColor, pulse);
                mat.SetColor(entry.colorProperty, c);

                // эмиссия (если есть)
                if (entry.hasEmission && mat.HasProperty("_EmissionColor"))
                {
                    Color targetEmission = targetColor * emissionIntensity;
                    Color e = Color.Lerp(entry.originalEmissionColor, targetEmission, pulse);
                    mat.SetColor("_EmissionColor", e);
                    mat.EnableKeyword("_EMISSION");
                }
            }

            yield return null;
        }

        // возвращаем исходные цвета и эмиссию
        foreach (var entry in matEntries)
        {
            if (entry.renderer == null) continue;

            var mats = entry.renderer.materials;
            if (entry.materialIndex < 0 || entry.materialIndex >= mats.Length) continue;

            var mat = mats[entry.materialIndex];
            if (mat == null || !mat.HasProperty(entry.colorProperty)) continue;

            mat.SetColor(entry.colorProperty, entry.originalColor);

            if (entry.hasEmission && mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", entry.originalEmissionColor);
            }
        }

        currentRoutine = null;
    }
}
