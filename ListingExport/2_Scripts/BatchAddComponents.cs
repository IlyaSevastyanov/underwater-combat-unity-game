using UnityEngine;
using UnityEditor;

public class BatchAddComponents : MonoBehaviour
{
    [MenuItem("Tools/Maze/Add MeshCollider To All Children")]
    private static void AddMeshColliderToChildren()
    {
        // 1. Нужно, чтобы был выделен корневой объект лабиринта
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("Выдели корневой объект лабиринта в Hierarchy и снова запусти команду.");
            return;
        }

        GameObject root = Selection.activeGameObject;
        int count = 0;

        // 2. Проходим по всем потомкам
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            // пропускаем сам корень, если не хотим трогать его
            if (child == root.transform) continue;

            // пример А: гарантировать, что у стены есть MeshFilter + MeshRenderer
            // (раскомментируй если это тебе надо)
            /*
            if (child.GetComponent<MeshFilter>() == null)
                child.gameObject.AddComponent<MeshFilter>();
            if (child.GetComponent<MeshRenderer>() == null)
                child.gameObject.AddComponent<MeshRenderer>();
            */

            // пример Б: добавить MeshCollider, если нет
            if (child.GetComponent<MeshCollider>() == null)
            {
                var col = child.gameObject.AddComponent<MeshCollider>();
                col.convex = false; // или true, если нужно физике игрока
                count++;
            }
        }

        Debug.Log($"Готово. Добавлено MeshCollider на {count} объектов под '{root.name}'.");
    }
}
