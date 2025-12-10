#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ScatterStonesOnSurface : EditorWindow
{
    GameObject stonePrefab;
    int count = 100;
    float minScale = 0.6f;
    float maxScale = 1.2f;
    float offsetFromSurface = 0.01f;
    int maxTriesPerStone = 50;
    bool alignToNormal = true;
    bool randomRotation = true;
    bool parentUnderTarget = true;
    bool useSelectedAsTargets = true;

    [MenuItem("Tools/Decor/Scatter Stones On Surface")]
    static void Open() => GetWindow<ScatterStonesOnSurface>("Scatter Stones");

    void OnGUI()
    {
        GUILayout.Label("Scatter stones onto selected meshes", EditorStyles.boldLabel);
        stonePrefab = (GameObject)EditorGUILayout.ObjectField("Stone Prefab", stonePrefab, typeof(GameObject), false);
        count = EditorGUILayout.IntField("Count per target", count);
        minScale = EditorGUILayout.FloatField("Min Scale", minScale);
        maxScale = EditorGUILayout.FloatField("Max Scale", maxScale);
        offsetFromSurface = EditorGUILayout.FloatField("Offset (m)", offsetFromSurface);
        maxTriesPerStone = EditorGUILayout.IntField("Max tries per stone", maxTriesPerStone);
        alignToNormal = EditorGUILayout.Toggle("Align to surface normal", alignToNormal);
        randomRotation = EditorGUILayout.Toggle("Random rotation around normal", randomRotation);
        parentUnderTarget = EditorGUILayout.Toggle("Parent under target", parentUnderTarget);
        useSelectedAsTargets = EditorGUILayout.Toggle("Use Selection as targets", useSelectedAsTargets);

        if (GUILayout.Button("Scatter Now"))
        {
            if (stonePrefab == null)
            {
                EditorUtility.DisplayDialog("Scatter Stones", "Assign stone prefab first.", "OK");
                return;
            }
            Scatter();
        }
    }

    void Scatter()
    {
        var targets = useSelectedAsTargets ? Selection.gameObjects : FindObjectsOfType<GameObject>();
        if (targets == null || targets.Length == 0) { EditorUtility.DisplayDialog("Scatter Stones", "No targets found (select objects).", "OK"); return; }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        int placedTotal = 0;
        foreach (var t in targets)
        {
            if (t == null) continue;
            var mf = t.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Mesh mesh = mf.sharedMesh;
            var verts = mesh.vertices;
            var tris = mesh.triangles;
            var normals = mesh.normals;

            System.Random rnd = new System.Random(); // deterministic? uses system seed

            for (int i = 0; i < count; i++)
            {
                bool placed = false;
                for (int tries = 0; tries < maxTriesPerStone && !placed; tries++)
                {
                    // pick random triangle
                    int triIndex = rnd.Next(0, tris.Length / 3);
                    int i0 = tris[triIndex * 3 + 0];
                    int i1 = tris[triIndex * 3 + 1];
                    int i2 = tris[triIndex * 3 + 2];

                    Vector3 v0 = verts[i0];
                    Vector3 v1 = verts[i1];
                    Vector3 v2 = verts[i2];

                    // random barycentric coordinates
                    float r1 = (float)rnd.NextDouble();
                    float r2 = (float)rnd.NextDouble();
                    if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }

                    Vector3 localPos = v0 + r1 * (v1 - v0) + r2 * (v2 - v0);
                    Vector3 worldPos = t.transform.TransformPoint(localPos);

                    // sample normal (interpolate)
                    Vector3 n0 = normals.Length > 0 ? normals[i0] : Vector3.up;
                    Vector3 n1 = normals.Length > 0 ? normals[i1] : Vector3.up;
                    Vector3 n2 = normals.Length > 0 ? normals[i2] : Vector3.up;
                    Vector3 localNormal = (n0 + n1 + n2).normalized;
                    Vector3 worldNormal = (t.transform.TransformDirection(localNormal)).normalized;

                    // raycast outward a bit to ensure not inside other geometry
                    Ray ray = new Ray(worldPos + worldNormal * 0.1f, -worldNormal);
                    if (Physics.Raycast(ray, out RaycastHit hit, 0.5f))
                    {
                        // optional: check hit.collider.gameObject == t to ensure surface belongs to target
                        // place slightly off surface along normal
                        Vector3 placePos = hit.point + worldNormal * offsetFromSurface;

                        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(stonePrefab);
                        Undo.RegisterCreatedObjectUndo(inst, "Scatter Stone");
                        inst.transform.position = placePos;
                        if (parentUnderTarget) inst.transform.SetParent(t.transform, true);

                        float s = Random.Range(minScale, maxScale);
                        inst.transform.localScale = Vector3.one * s;

                        // orientation
                        if (alignToNormal)
                        {
                            inst.transform.rotation = Quaternion.FromToRotation(Vector3.up, worldNormal);
                            if (randomRotation)
                            {
                                inst.transform.Rotate(worldNormal, Random.Range(0f, 360f), Space.World);
                            }
                        }
                        else if (randomRotation)
                        {
                            inst.transform.rotation = Random.rotation;
                        }

                        placed = true;
                        placedTotal++;
                    }
                } // tries
            } // count
        } // targets

        Undo.CollapseUndoOperations(group);
        EditorUtility.DisplayDialog("Scatter Stones", $"Placed ~{placedTotal} stones.", "OK");
    }
}
#endif
