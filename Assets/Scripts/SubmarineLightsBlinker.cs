using UnityEngine;

public class SubmarineLightsBlinker : MonoBehaviour
{
    [Header("Renderers")]
    public Renderer sideLampsRenderer; // жёлтые боковые
    public Renderer topLampsRenderer;  // зелёные верхние

    [Header("Warmth (Кельвины)")]
    [Range(1500, 6500)] public float sideTemperatureK = 2600f; // тёплая лампа накаливания
    [Range(1500, 6500)] public float topTemperatureK = 3200f; // чуть холоднее для верха
    [Range(0f, 1f)] public float desaturate = 0.35f;       // снижение насыщенности (0.3–0.5)

    [Header("Blink & Intensity")]
    public float sideBlinkSpeed = 1.6f;
    public float topBlinkSpeed = 1.0f;

    // понизил по умолчанию — меньше «неона». Подбирай 0.6–1.8
    [Range(0f, 5f)] public float sideEmission = 1.0f;
    [Range(0f, 5f)] public float topEmission = 0.8f;

    [Tooltip("Смягчает мигание, чтобы не било по глазам и блуму")]
    public AnimationCurve pulseShape = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Top (зелёные) — потеплее, но всё ещё зелёные")]
    [Range(0f, 1f)] public float topWarmMix = 0.55f; // смешиваем зелёный с тёплым белым

    [Header("Advanced")]
    [Tooltip("Менять ли базовый цвет материала. По умолчанию — нет, чтобы избежать «неона».")]
    public bool alsoAffectBaseColor = false;

    private MaterialPropertyBlock _mpbSide, _mpbTop;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorID = Shader.PropertyToID("_Color");

    void Awake()
    {
        _mpbSide ??= new MaterialPropertyBlock();
        _mpbTop ??= new MaterialPropertyBlock();
        EnableEmissionKeyword(sideLampsRenderer);
        EnableEmissionKeyword(topLampsRenderer);
    }

    void Update()
    {
        // --- БОКОВЫЕ (тёплый янтарный) ---
        if (sideLampsRenderer)
        {
            float t = 0.5f + 0.5f * Mathf.Sin(Time.time * sideBlinkSpeed * Mathf.PI * 2f);
            t = pulseShape.Evaluate(t); // сгладили синус
            Color warm = KelvinToRGB(sideTemperatureK);
            warm = Desaturate(warm, desaturate);

            // немного «не ноль» даже в off, чтобы не было полного провала
            float offLevel = 0.08f;
            float pulse = Mathf.Lerp(offLevel, 1f, t);
            Color emission = warm * (sideEmission * pulse);

            Apply(sideLampsRenderer, _mpbSide, emission, alsoAffectBaseColor ? warm : (Color?)null);
        }

        // --- ВЕРХНИЕ (зелёные, но теплее/желтее) ---
        if (topLampsRenderer)
        {
            float t = 0.5f + 0.5f * Mathf.Sin(Time.time * topBlinkSpeed * Mathf.PI * 2f);
            t = pulseShape.Evaluate(t);

            // базовый зелёный
            Color green = new Color(0f, 1f, 0.2f, 1f);
            // тёплый белый
            Color warmWhite = KelvinToRGB(topTemperatureK);
            // смешали — получился «оливково-жёлто-зелёный», мягче
            Color topWarm = Color.Lerp(green, warmWhite, topWarmMix);
            topWarm = Desaturate(topWarm, desaturate);

            float offLevel = 0.06f;
            float pulse = Mathf.Lerp(offLevel, 1f, t);
            Color emission = topWarm * (topEmission * pulse);

            Apply(topLampsRenderer, _mpbTop, emission, alsoAffectBaseColor ? topWarm : (Color?)null);
        }
    }

    // --- Helpers ---

    private void Apply(Renderer r, MaterialPropertyBlock mpb, Color emissionLinear, Color? baseColor = null)
    {
        if (!r) return;

        r.GetPropertyBlock(mpb);

        // Эмиссия — лучше в ЛИНЕЙНОМ, чтобы не «неонить» от гаммы
        // (Unity сам учтёт пространство, но явная конверсия убирает сюрпризы)
        mpb.SetColor(EmissionColorID, emissionLinear.linear);

        if (baseColor.HasValue)
        {
            // albedo — не усиливаем, без умножения на интенсивность
            mpb.SetColor(BaseColorID, baseColor.Value);
        }

        r.SetPropertyBlock(mpb);
    }

    private void EnableEmissionKeyword(Renderer r)
    {
        if (!r) return;
        var mats = r.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
        {
            var m = mats[i];
            if (!m) continue;
            if (!m.IsKeywordEnabled("_EMISSION"))
                m.EnableKeyword("_EMISSION");
        }
    }

    // Упрощённая конверсия «температура К» → RGB (достаточно для стилизации)
    private static Color KelvinToRGB(float kelvin)
    {
        kelvin = Mathf.Clamp(kelvin, 1000f, 15000f) / 100f;
        float r, g, b;

        // Red
        if (kelvin <= 66f) r = 1f;
        else r = Mathf.Clamp01(1.292936186062745f * Mathf.Pow(kelvin - 60f, -0.1332047592f));

        // Green
        if (kelvin <= 66f)
            g = Mathf.Clamp01(0.3900815787690196f * Mathf.Log(kelvin) - 0.6318414437886275f);
        else
            g = Mathf.Clamp01(1.129890860895294f * Mathf.Pow(kelvin - 60f, -0.0755148492f));

        // Blue
        if (kelvin >= 66f) b = 1f;
        else if (kelvin <= 19f) b = 0f;
        else b = Mathf.Clamp01(0.5432067891101961f * Mathf.Log(kelvin - 10f) - 1.19625408914f);

        return new Color(r, g, b, 1f);
    }

    private static Color Desaturate(Color c, float amount)
    {
        // в HSV понижаем S, чтобы уйти от кислотности
        Color.RGBToHSV(c, out float h, out float s, out float v);
        s = Mathf.Lerp(s, 0f, Mathf.Clamp01(amount));
        return Color.HSVToRGB(h, s, v);
    }
}
