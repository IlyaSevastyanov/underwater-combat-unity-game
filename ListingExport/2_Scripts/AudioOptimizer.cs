using UnityEditor;
using UnityEngine;

public static class AudioOptimizer
{
    private const string SkipToken = "_NoOptAudio";

    [MenuItem("Tools/Audio Optimizer/Preview")]
    private static void Preview()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip");
        int count = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) continue;
            if (path.Contains(SkipToken)) continue;

            var settings = importer.defaultSampleSettings;

            bool needChange =
                settings.loadType != AudioClipLoadType.Streaming ||
                settings.compressionFormat != AudioCompressionFormat.Vorbis ||
                settings.quality > 0.6f; // 0..1

            if (needChange)
            {
                Debug.Log($"[Preview Audio] {path}  loadType={settings.loadType}  fmt={settings.compressionFormat}  q={settings.quality}");
                count++;
            }
        }

        Debug.Log($"[AudioOptimizer] Preview done. Clips to change: {count}");
    }

    [MenuItem("Tools/Audio Optimizer/Apply Mobile Settings")]
    private static void Apply()
    {
        if (!EditorUtility.DisplayDialog(
                "Audio Optimizer",
                "Оптимизировать аудио под мобилки (Streaming + Vorbis 0.5)?\n" +
                "Сделай бэкап проекта на всякий случай.",
                "Да", "Отмена"))
            return;

        string[] guids = AssetDatabase.FindAssets("t:AudioClip");
        int modified = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                if (path.Contains(SkipToken)) continue;

                var settings = importer.defaultSampleSettings;

                bool changed = false;

                // длинные треки → Streaming, короткие SFX можно оставить DecompressOnLoad
                settings.loadType = AudioClipLoadType.Streaming;

                // Vorbis — нормальный формат для музыки/эффектов
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.5f; // 0..1

                importer.defaultSampleSettings = settings;
                changed = true;

                if (changed)
                {
                    Debug.Log($"[AudioOptimizer] Modified: {path}");
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    modified++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        Debug.Log($"[AudioOptimizer] Done. Modified clips: {modified}");
    }
}
