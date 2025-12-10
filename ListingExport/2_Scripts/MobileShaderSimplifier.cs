using UnityEngine;

/// <summary>
/// На мобилках заменяет только указанные "тяжёлые" шейдеры на простые.
/// Повесь на один объект в сцене (например, System).
/// </summary>
public class MobileShaderReplacer : MonoBehaviour
{
    [System.Serializable]
    public class ShaderMapping
    {
        [Tooltip("Исходный ТЯЖЁЛЫЙ шейдер, который хотим заменить (например, подводный эффект и т.п.)")]
        public Shader sourceShader;

        [Tooltip("Более простой шейдер для мобилок (например, Unlit/Texture или Mobile/Diffuse)")]
        public Shader mobileShader;
    }

    [Header("Какие шейдеры заменяем на мобилках")]
    public ShaderMapping[] mappings;

    [Header("Область действия")]
    [Tooltip("Обрабатывать всех Renderer во всей сцене (FindObjectsOfType). " +
             "Если выключено — только детей этого объекта.")]
    public bool affectWholeScene = true;

    [Header("Отладка")]
    [Tooltip("В редакторе принудительно включать мобильный режим (для проверки).")]
    public bool forceInEditor = false;

    [Tooltip("Писать в лог, какие материалы были заменены.")]
    public bool logChanges = true;


    void Awake()
    {
        if (!ShouldRun())
        {
            enabled = false;
            return;
        }

        if (mappings == null || mappings.Length == 0)
        {
            Debug.LogWarning("[MobileShaderReplacer] Нет настроенных маппингов шейдеров, ничего не делаю.");
            enabled = false;
            return;
        }

        ReplaceShaders();
    }

    bool ShouldRun()
    {
        // Здесь можно завязаться на твой WebGLBrowserCheck
#if UNITY_WEBGL && !UNITY_EDITOR
        // Если используешь JS-плагин:
        // return WebGLBrowserCheck.IsMobileBrowser();
        return Application.isMobilePlatform;
#elif UNITY_ANDROID || UNITY_IOS
        return true;
#else
        // В редакторе только если включён forceInEditor
        return Application.isEditor && forceInEditor;
#endif
    }

    void ReplaceShaders()
    {
        Renderer[] renderers;

        if (affectWholeScene)
            renderers = FindObjectsOfType<Renderer>(true);
        else
            renderers = GetComponentsInChildren<Renderer>(true);

        int replacedCount = 0;

        foreach (var rend in renderers)
        {
            if (!rend) continue;

            var mats = rend.sharedMaterials;
            if (mats == null || mats.Length == 0) continue;

            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (!mat || mat.shader == null) continue;

                // Проверяем, попадает ли шейдер в список маппингов
                ShaderMapping mapping = FindMappingFor(mat.shader);
                if (mapping == null || mapping.mobileShader == null)
                    continue;

                // уже заменён?
                if (mat.shader == mapping.mobileShader)
                    continue;

                if (logChanges)
                {
                    Debug.Log($"[MobileShaderReplacer] {rend.name} / {mat.name}: " +
                              $"{mat.shader.name} → {mapping.mobileShader.name}", rend);
                }

                mat.shader = mapping.mobileShader;
                replacedCount++;
            }
        }

        Debug.Log($"[MobileShaderReplacer] Заменены шейдеры у {replacedCount} материалов.");
    }

    ShaderMapping FindMappingFor(Shader src)
    {
        if (src == null || mappings == null) return null;

        foreach (var m in mappings)
        {
            if (m != null && m.sourceShader == src)
                return m;
        }
        return null;
    }
}
