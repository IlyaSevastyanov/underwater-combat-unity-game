#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

public static class ListingExporter
{
    // Какие настройки ты хочешь включать в листинг
    // Добавь сюда те, что реально нужны в твоём документе
    static readonly string[] ProjectSettingsFiles =
    {
        "ProjectSettings/ProjectSettings.asset",
        "ProjectSettings/InputManager.asset",
        "ProjectSettings/DynamicsManager.asset",
        "ProjectSettings/TagManager.asset",
        "ProjectSettings/TimeManager.asset",
        "ProjectSettings/QualitySettings.asset",
        "ProjectSettings/GraphicsSettings.asset",
        "ProjectSettings/Physics2DSettings.asset",
        "ProjectSettings/AudioManager.asset",
        "ProjectSettings/URPProjectSettings.asset",          // если есть
        "ProjectSettings/ShaderGraphSettings.asset",         // если есть
    };

    [MenuItem("Tools/Listing/Export Code Listing")]
    public static void Export()
    {
        // Корневая папка выгрузки
        string exportRoot = Path.Combine(Application.dataPath, "../ListingExport");
        string settingsOut = Path.Combine(exportRoot, "1_ProjectSettings");
        string scriptsOut  = Path.Combine(exportRoot, "2_Scripts");

        EnsureCleanDir(exportRoot);
        Directory.CreateDirectory(settingsOut);
        Directory.CreateDirectory(scriptsOut);

        // 1) Копируем ProjectSettings
        var exportedSettings = new List<string>();
        foreach (var relPath in ProjectSettingsFiles)
        {
            string fullPath = Path.Combine(Application.dataPath, "../", relPath);
            if (!File.Exists(fullPath))
                continue;

            string fileName = Path.GetFileName(relPath);
            string dst = Path.Combine(settingsOut, fileName);

            File.Copy(fullPath, dst, true);
            exportedSettings.Add(fileName);
        }

        // 2) Находим все .cs в Assets
        string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });

        // Преобразуем в пути и фильтруем Editor-скрипты по желанию
        var scriptPaths = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".cs"))
            .OrderBy(p => p)
            .ToList();

        // 3) Копируем тексты скриптов в папку 2_Scripts
        var exportedScripts = new List<string>();

        foreach (var assetPath in scriptPaths)
        {
            // Можно исключить Editor-скрипты из "боевого" листинга:
            // if (assetPath.Contains("/Editor/")) continue;

            string fileName = Path.GetFileName(assetPath);
            string srcFull = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            string dstFull = Path.Combine(scriptsOut, fileName);

            if (!File.Exists(srcFull))
                continue;

            File.Copy(srcFull, dstFull, true);
            exportedScripts.Add(fileName);
        }

        // 4) Генерируем СОДЕРЖАНИЕ
        string tocPath = Path.Combine(exportRoot, "СОДЕРЖАНИЕ.txt");
        File.WriteAllText(tocPath, BuildTOC(exportedSettings, exportedScripts), Encoding.UTF8);

        // 5) Генерируем один общий файл листинга (удобно для Word)
        string allPath = Path.Combine(exportRoot, "Листинг_все_файлы.txt");
        File.WriteAllText(allPath, BuildCombinedListing(settingsOut, scriptsOut, exportedSettings, exportedScripts), Encoding.UTF8);

        Debug.Log($"[ListingExporter] Done. Export folder: {exportRoot}");
        EditorUtility.RevealInFinder(exportRoot);
    }

    static void EnsureCleanDir(string dir)
    {
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
    }

    static string BuildTOC(List<string> settings, List<string> scripts)
    {
        var sb = new StringBuilder();

        sb.AppendLine("СОДЕРЖАНИЕ");
        sb.AppendLine("1 ТЕКСТЫ ПРОГРАММЫ НАСТРОЙКИ ПРОЕКТА");

        for (int i = 0; i < settings.Count; i++)
        {
            sb.AppendLine($"1.{i + 1} Текст файла данных «{settings[i]}».");
        }

        sb.AppendLine("2 ТЕКСТЫ ПРОГРАММ СКРИПТОВ");

        for (int i = 0; i < scripts.Count; i++)
        {
            sb.AppendLine($"2.{i + 1} Текст программы «{scripts[i]}».");
        }

        return sb.ToString();
    }

    static string BuildCombinedListing(
        string settingsOut,
        string scriptsOut,
        List<string> settings,
        List<string> scripts)
    {
        var sb = new StringBuilder();

        sb.AppendLine("1 ТЕКСТЫ ПРОГРАММЫ НАСТРОЙКИ ПРОЕКТА");

        for (int i = 0; i < settings.Count; i++)
        {
            string file = settings[i];
            string full = Path.Combine(settingsOut, file);

            sb.AppendLine();
            sb.AppendLine($"1.{i + 1} Текст файла данных «{file}».");
            sb.AppendLine("Назначение: файл настроек проекта Unity.");
            sb.AppendLine("Язык разметки: YAML.");
            sb.AppendLine("Код разметки:");

            if (File.Exists(full))
            {
                sb.AppendLine(File.ReadAllText(full));
            }
        }

        sb.AppendLine();
        sb.AppendLine("2 ТЕКСТЫ ПРОГРАММ СКРИПТОВ");

        for (int i = 0; i < scripts.Count; i++)
        {
            string file = scripts[i];
            string full = Path.Combine(scriptsOut, file);

            sb.AppendLine();
            sb.AppendLine($"2.{i + 1} Текст программы «{file}».");
            sb.AppendLine("Назначение: исходный код компонента игрового проекта.");
            sb.AppendLine("Язык программирования: C#.");
            sb.AppendLine("Код программы:");

            if (File.Exists(full))
            {
                sb.AppendLine(File.ReadAllText(full));
            }
        }

        return sb.ToString();
    }
}
#endif

