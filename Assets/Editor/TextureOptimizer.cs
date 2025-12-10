// Assets/Editor/TextureOptimizer.cs
using UnityEditor;
using UnityEngine;

public static class TextureOptimizer
{
    /// <summary>
    /// Текстуры, в названии которых есть это слово, будут пропущены.
    /// Например: UI_Logo_NoOpt.png
    /// </summary>
    private const string SkipToken = "_NoOpt";

    // Базовые настройки
    private const int TargetMaxSize = 1024;                 // целевой Max Size
    private const int UiMaxSize = 512;                      // для UI-иконок поменьше
    private const int CompressionQuality = 50;              // 0..100 (Crunched)

    // --- ПРОСМОТР БЕЗ ИЗМЕНЕНИЙ ---
    [MenuItem("Tools/Texture Optimizer/Preview")]
    private static void PreviewOptimize()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        int count = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            if (path.Contains(SkipToken))
                continue;

            int currentMax = importer.maxTextureSize;

            bool willChangeMaxSize = currentMax > TargetMaxSize;
            bool willChangeCompression =
                importer.textureCompression == TextureImporterCompression.Uncompressed;

            if (willChangeMaxSize || willChangeCompression)
            {
                Debug.Log($"[Preview] {path}  MaxSize:{currentMax}  Compression:{importer.textureCompression}");
                count++;
            }
        }

        Debug.Log($"[TextureOptimizer] Preview finished. Textures to change: {count}");
    }

    // --- ПРИМЕНИТЬ БАЗОВУЮ ОПТИМИЗАЦИЮ ---
    [MenuItem("Tools/Texture Optimizer/Apply Basic Mobile Settings")]
    private static void ApplyOptimize()
    {
        if (!EditorUtility.DisplayDialog(
                "Texture Optimizer",
                "Применить базовую оптимизацию текстур? " +
                "Лучше сделать резервную копию проекта перед этим.",
                "Да, оптимизировать",
                "Отмена"))
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D");

        int modified = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                // Пропускаем отмеченные
                if (path.Contains(SkipToken))
                    continue;

                bool isUI =
                    importer.textureType == TextureImporterType.Sprite &&
                    importer.spritePixelsPerUnit >= 100; // грубый признак иконок/UI

                int targetSize = isUI ? UiMaxSize : TargetMaxSize;

                bool changed = false;

                // Max Size
                if (importer.maxTextureSize > targetSize)
                {
                    importer.maxTextureSize = targetSize;
                    changed = true;
                }

                // Для обычных текстур включаем мипмапы
                if (importer.textureType == TextureImporterType.Default ||
                    importer.textureType == TextureImporterType.NormalMap)
                {
                    if (!importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = true;
                        changed = true;
                    }
                }

                // Сжатие
                if (importer.textureCompression != TextureImporterCompression.Compressed &&
                    importer.textureCompression != TextureImporterCompression.CompressedHQ)
                {
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    changed = true;
                }

                // Crunched compression ( даёт ещё минус по размеру, но подольше импорт )
                if (!importer.crunchedCompression)
                {
                    importer.crunchedCompression = true;
                    importer.compressionQuality = CompressionQuality;
                    changed = true;
                }

                if (changed)
                {
                    Debug.Log($"[TextureOptimizer] Modified: {path}");
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    modified++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        Debug.Log($"[TextureOptimizer] Done. Modified textures: {modified}");
    }
}
