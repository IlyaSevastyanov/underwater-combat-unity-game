#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

public class CombineStaticMeshesByMaterial : EditorWindow
{
    [MenuItem("Tools/Vegetation/Combine Static Meshes By Material")]
    static void Open() => GetWindow<CombineStaticMeshesByMaterial>("Combine Static Meshes");

    public bool useSelection = true;
    public Transform parent;
    public bool includeInactive = false;
    public bool keepOriginalsDisabled = true; // отключать исходники после комбинирования
    public bool generateSecondaryUV = false;   // сгенерировать UV2 перед комбинированием
    public bool setIndexFormatTo32 = true;     // включать 32-bit индексы при необходимости
    public bool markCombinedStatic = true;     // помечать комбинированные объекты как Static
    public bool removeOriginalRenderers = false; // удалить MeshRenderer у оригиналов (вместо отключения)

    void OnGUI()
    {
        GUILayout.Label("Targets", EditorStyles.boldLabel);
        useSelection = EditorGUILayout.Toggle("Use Selection", useSelection);
        EditorGUI.BeginDisabledGroup(useSelection);
        parent = (Transform)EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true);
        EditorGUI.EndDisabledGroup();
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);

        EditorGUILayout.Space();
        GUILayout.Label("Options", EditorStyles.boldLabel);
        generateSecondaryUV = EditorGUILayout.Toggle("Generate Secondary UV (UV2)", generateSecondaryUV);
        setIndexFormatTo32 = EditorGUILayout.Toggle("Allow 32-bit indices if needed", setIndexFormatTo32);
        keepOriginalsDisabled = EditorGUILayout.Toggle("Keep originals disabled (undoable)", keepOriginalsDisabled);
        removeOriginalRenderers = EditorGUILayout.Toggle("Remove original MeshRenderers (permanent)", removeOriginalRenderers);
        markCombinedStatic = EditorGUILayout.Toggle("Mark combined objects Static", markCombinedStatic);

        EditorGUILayout.Space();
        if (GUILayout.Button("Combine Now"))
        {
            CombineNow();
        }
    }

    void CombineNow()
    {
        var roots = CollectRoots().ToArray();
        if (roots.Length == 0)
        {
            EditorUtility.DisplayDialog("Combine Static Meshes", "No targets found. Select objects or assign Parent.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        int combinedObjects = 0;
        List<GameObject> created = new List<GameObject>();

        foreach (var rootGO in roots)
        {
            var filters = rootGO.GetComponentsInChildren<MeshFilter>(includeInactive)
                              .Where(f => f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null)
                              .ToArray();

            // Build list of (mesh, renderer, transform, submeshIndex) entries so we can group by material
            var entries = new List<MeshEntry>();
            foreach (var mf in filters)
            {
                var mr = mf.GetComponent<MeshRenderer>();
                var mesh = mf.sharedMesh;
                var mats = mr.sharedMaterials;
                for (int sub = 0; sub < Mathf.Min(mesh.subMeshCount, mats.Length); sub++)
                {
                    var mat = mats[sub];
                    if (mat == null) continue;
                    entries.Add(new MeshEntry { mesh = mesh, renderer = mr, filter = mf, transform = mf.transform, submesh = sub, material = mat });
                }
            }

            // Group by material instance ID
            var groups = entries.GroupBy(e => e.material.GetInstanceID());

            foreach (var g in groups)
            {
                var groupList = g.ToList();
                var firstMat = groupList[0].material;
                string combinedName = $"Combined_{firstMat.name}_{rootGO.name}";

                List<CombineInstance> combineInstances = new List<CombineInstance>();
                int totalVertexCount = 0;
                foreach (var e in groupList)
                {
                    var ci = new CombineInstance();
                    ci.mesh = e.mesh;
                    ci.subMeshIndex = e.submesh;
                    // transform relative to root: worldToLocal(root) * localToWorld(source)
                    ci.transform = rootGO.transform.worldToLocalMatrix * e.transform.localToWorldMatrix;
                    combineInstances.Add(ci);
                    totalVertexCount += e.mesh.vertexCount;
                }

                if (combineInstances.Count == 0) continue;

                // Optionally generate UV2 for each source mesh (best effort)
                if (generateSecondaryUV)
                {
                    // We'll replace meshes referenced in combineInstances by copies that have UV2 generated.
                    for (int i = 0; i < groupList.Count; i++)
                    {
                        var e = groupList[i];
                        Mesh src = e.filter.sharedMesh;
                        Mesh temp = Object.Instantiate(src);
                        Unwrapping.GenerateSecondaryUVSet(temp);

                        // replace mesh references in combineInstances where mesh == src
                        for (int k = 0; k < combineInstances.Count; k++)
                        {
                            var tmpCI = combineInstances[k];
                            if (tmpCI.mesh == src)
                            {
                                tmpCI.mesh = temp;
                                combineInstances[k] = tmpCI; // assign back the struct
                            }
                        }
                    }
                }

                // Create combined mesh
                Mesh combinedMesh = new Mesh();
                if (setIndexFormatTo32)
                {
                    if (totalVertexCount > 65534) combinedMesh.indexFormat = IndexFormat.UInt32;
                }

                combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true, true);

                // Create GameObject for combined mesh
                GameObject go = new GameObject(combinedName);
                Undo.RegisterCreatedObjectUndo(go, "Create Combined Mesh");
                go.transform.SetParent(rootGO.transform, false);
                var mfNew = go.AddComponent<MeshFilter>();
                mfNew.sharedMesh = combinedMesh;
                var mrNew = go.AddComponent<MeshRenderer>();
                mrNew.sharedMaterial = firstMat;

                if (markCombinedStatic)
                {
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ContributeGI);
                }

                created.Add(go);
                combinedObjects++;

                // disable or remove originals
                foreach (var e in groupList)
                {
                    var origGO = e.transform.gameObject;
                    if (removeOriginalRenderers)
                    {
                        var r = origGO.GetComponent<MeshRenderer>();
                        if (r) Undo.DestroyObjectImmediate(r);
                    }
                    else
                    {
                        if (keepOriginalsDisabled)
                        {
                            Undo.RecordObject(origGO, "Disable original GO");
                            origGO.SetActive(false);
                        }
                        else
                        {
                            Undo.DestroyObjectImmediate(origGO);
                        }
                    }
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[CombineStatic] Created {combinedObjects} combined objects.");
        EditorUtility.DisplayDialog("Combine Static Meshes", $"Created {combinedObjects} combined objects.", "OK");
    }

    IEnumerable<GameObject> CollectRoots()
    {
        if (useSelection)
        {
            return Selection.gameObjects
                            .Where(g => g != null)
                            .Distinct();
        }
        else if (parent != null)
        {
            return new[] { parent.gameObject };
        }
        else
        {
            return System.Linq.Enumerable.Empty<GameObject>();
        }
    }

    class MeshEntry
    {
        public Mesh mesh;
        public MeshRenderer renderer;
        public MeshFilter filter;
        public Transform transform;
        public int submesh;
        public Material material;
    }
}
#endif
