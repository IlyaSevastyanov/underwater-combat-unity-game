// Assets/Editor/VegetationBatchOptimizer.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class VegetationBatchOptimizer : EditorWindow
{
    [MenuItem("Tools/Vegetation/Batch Optimizer")]
    static void Open() => GetWindow<VegetationBatchOptimizer>("Vegetation Optimizer");

    // Targeting
    public bool useSelection = true;
    public Transform parent;
    public bool includeInactive = true;

    // Actions
    public bool disableShadows = true;
    public bool disableReceiveShadows = true;
    public bool disableLightProbes = true;
    public bool disableReflectionProbes = true;
    public bool removeColliders = true;

    // Static flags
    public bool setBatchingStatic = true;
    public bool setOccludeeStatic = true;
    public bool setOccluderStatic = false;     // обычно не нужно для растительности
    public bool setContributeGI = false;       // включай по необходимости

    // UI
    void OnGUI()
    {
        GUILayout.Label("Targets", EditorStyles.boldLabel);
        useSelection = EditorGUILayout.Toggle("Use Selection", useSelection);
        EditorGUI.BeginDisabledGroup(useSelection);
        parent = (Transform)EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true);
        EditorGUI.EndDisabledGroup();
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);

        EditorGUILayout.Space();
        GUILayout.Label("Actions", EditorStyles.boldLabel);
        disableShadows = EditorGUILayout.Toggle("Disable Cast Shadows", disableShadows);
        disableReceiveShadows = EditorGUILayout.Toggle("Disable Receive Shadows", disableReceiveShadows);
        disableLightProbes = EditorGUILayout.Toggle("Disable Light Probes", disableLightProbes);
        disableReflectionProbes = EditorGUILayout.Toggle("Disable Reflection Probes", disableReflectionProbes);
        removeColliders = EditorGUILayout.Toggle("Remove Colliders", removeColliders);

        EditorGUILayout.Space();
        GUILayout.Label("Static Flags", EditorStyles.boldLabel);
        setBatchingStatic = EditorGUILayout.Toggle("Batching Static", setBatchingStatic);
        setOccludeeStatic = EditorGUILayout.Toggle("Occludee Static", setOccludeeStatic);
        setOccluderStatic = EditorGUILayout.Toggle("Occluder Static", setOccluderStatic);
        setContributeGI   = EditorGUILayout.Toggle("Contribute GI", setContributeGI);

        EditorGUILayout.Space();
        if (GUILayout.Button("Optimize Now"))
        {
            OptimizeNow();
        }
    }

    void OptimizeNow()
    {
        var roots = CollectRoots().ToArray();
        if (roots.Length == 0)
        {
            EditorUtility.DisplayDialog("Vegetation Optimizer", "No targets found. Select objects or assign Parent.", "OK");
            return;
        }

        int renderersTouched = 0;
        int collidersRemoved = 0;
        int objectsFlaggedStatic = 0;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        foreach (var root in roots)
        {
            var gos = root.GetComponentsInChildren<Transform>(includeInactive)
                          .Select(t => t.gameObject);

            foreach (var go in gos)
            {
                // Renderers: тени/пробы
                var rends = go.GetComponents<Renderer>();
                foreach (var r in rends)
                {
                    if (!r) continue;
                    bool changed = false;
                    Undo.RecordObject(r, "Optimize Renderer");

                    if (disableShadows && r.shadowCastingMode != ShadowCastingMode.Off)
                    {
                        r.shadowCastingMode = ShadowCastingMode.Off;
                        changed = true;
                    }
                    if (disableReceiveShadows && r.receiveShadows)
                    {
                        r.receiveShadows = false;
                        changed = true;
                    }
                    if (disableLightProbes && r.lightProbeUsage != LightProbeUsage.Off)
                    {
                        r.lightProbeUsage = LightProbeUsage.Off;
                        changed = true;
                    }
                    if (disableReflectionProbes && r.reflectionProbeUsage != ReflectionProbeUsage.Off)
                    {
                        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                        changed = true;
                    }

                    if (changed) renderersTouched++;
                }

                // Colliders: удалить
                if (removeColliders)
                {
                    var cols = go.GetComponents<Collider>();
                    foreach (var c in cols)
                    {
                        if (!c) continue;
                        collidersRemoved++;
                        Undo.DestroyObjectImmediate(c);
                    }
                }

                // Static flags
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                StaticEditorFlags before = flags;

                if (setBatchingStatic)  flags |= StaticEditorFlags.BatchingStatic;
                if (setOccludeeStatic)  flags |= StaticEditorFlags.OccludeeStatic;
                if (setOccluderStatic)  flags |= StaticEditorFlags.OccluderStatic;
                if (setContributeGI)    flags |= StaticEditorFlags.ContributeGI;

                if (flags != before)
                {
                    Undo.RegisterCompleteObjectUndo(go, "Set Static Flags");
                    GameObjectUtility.SetStaticEditorFlags(go, flags);
                    objectsFlaggedStatic++;
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        string msg = $"Optimized:\nRenderers changed: {renderersTouched}\nColliders removed: {collidersRemoved}\nObjects flagged static: {objectsFlaggedStatic}";
        Debug.Log($"[Vegetation Optimizer] {msg}");
        ShowNotification(new GUIContent("Optimization complete"));
        EditorUtility.DisplayDialog("Vegetation Optimizer", msg, "OK");
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
            return Enumerable.Empty<GameObject>();
        }
    }
}
#endif
