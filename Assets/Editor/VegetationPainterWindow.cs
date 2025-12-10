// Assets/Editor/VegetationPainterWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class VegetationPainterWindow : EditorWindow
{
    [MenuItem("Tools/Vegetation Painter")]
    static void Open() => GetWindow<VegetationPainterWindow>("Vegetation Painter");

    // What to paint
    public List<GameObject> prefabs = new List<GameObject>();

    // Placement
    public Transform parent;
    public LayerMask paintMask = ~0;
    public float brushRadius = 2f;
    public int countPerStroke = 10;

    // Rotation/scale
    public bool preserveRotation = true;   // не менять ориентацию префаба
    public bool alignToNormal = true;      // выравнивать по нормали (если preserveRotation = false)
    public bool randomYaw = true;          // случайный поворот вокруг оси
    public Vector2 uniformScale = new Vector2(0.8f, 1.2f);

    // Filters
    [Range(0, 90)] public float maxSlope = 60f; // 0..90, работает для верхних и нижних сторон
    public float minSpacing = 0.35f;
    public bool avoidOverlap = true;

    // Ray modes
    public enum RayDirectionMode { Auto, SurfaceNormal, WorldDown, Camera }
    public RayDirectionMode rayMode = RayDirectionMode.Auto;

    // Gizmos
    public Color brushColor = new Color(0f, 1f, 0.6f, 0.25f);

    // runtime
    RaycastHit _hit;
    bool _hasHit;
    readonly List<Transform> _tmpChildren = new List<Transform>(1024);

    void OnEnable()  => SceneView.duringSceneGui += OnSceneGUI;
    void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    void OnGUI()
    {
        var so = new SerializedObject(this);
        EditorGUILayout.PropertyField(so.FindProperty("prefabs"), true);

        parent = (Transform)EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true);
        paintMask = LayerMaskField("Paint Mask", paintMask);

        brushRadius = EditorGUILayout.Slider("Brush Radius", brushRadius, 0.2f, 20f);
        countPerStroke = EditorGUILayout.IntSlider("Count/Stroke", countPerStroke, 1, 200);

        preserveRotation = EditorGUILayout.Toggle("Preserve Prefab Rotation", preserveRotation);
        EditorGUI.BeginDisabledGroup(preserveRotation);
        alignToNormal = EditorGUILayout.Toggle("Align To Normal", alignToNormal);
        randomYaw     = EditorGUILayout.Toggle("Random Yaw", randomYaw);
        EditorGUI.EndDisabledGroup();

        uniformScale = EditorGUILayout.Vector2Field("Uniform Scale Min/Max", uniformScale);

        maxSlope = EditorGUILayout.Slider("Max Slope", maxSlope, 0, 90);
        minSpacing = EditorGUILayout.Slider("Min Spacing", minSpacing, 0f, 2f);
        avoidOverlap = EditorGUILayout.Toggle("Avoid Overlap", avoidOverlap);

        rayMode = (RayDirectionMode)EditorGUILayout.EnumPopup("Ray Mode", rayMode);

        brushColor = EditorGUILayout.ColorField("Brush Color", brushColor);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Ctrl + LMB: paint\nShift + LMB: erase", MessageType.Info);

        so.ApplyModifiedProperties();
    }

    // Без UnityEditorInternal — совместимо везде
    static LayerMask LayerMaskField(string label, LayerMask selected)
    {
        var names = new List<string>();
        var numbers = new List<int>();
        for (int i = 0; i < 32; i++)
        {
            string n = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(n)) { names.Add(n); numbers.Add(i); }
        }

        int maskNoEmpty = 0;
        for (int i = 0; i < numbers.Count; i++)
            if (((selected.value >> numbers[i]) & 1) == 1)
                maskNoEmpty |= (1 << i);

        maskNoEmpty = EditorGUILayout.MaskField(label, maskNoEmpty, names.ToArray());

        int mask = 0;
        for (int i = 0; i < numbers.Count; i++)
            if ((maskNoEmpty & (1 << i)) != 0)
                mask |= 1 << numbers[i];

        selected.value = mask;
        return selected;
    }

    void OnSceneGUI(SceneView view)
    {
        Event e = Event.current;

        // Ray under mouse
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        _hasHit = Physics.Raycast(ray, out _hit, 10000f, paintMask, QueryTriggerInteraction.Ignore);

        // Brush gizmo
        if (_hasHit)
        {
            Handles.color = brushColor;
            Handles.DrawSolidDisc(_hit.point, _hit.normal, brushRadius);
            Handles.color = Color.black;
            Handles.DrawWireDisc(_hit.point, _hit.normal, brushRadius);
            view.Repaint();
        }

        bool leftDown = e.type == EventType.MouseDown && e.button == 0;
        bool leftDrag = e.type == EventType.MouseDrag && e.button == 0;
        bool painting = e.control && !e.shift; // Ctrl
        bool erasing  = e.shift;

        // Едим ввод сцены только когда рисуем/стираем
        if (painting || erasing)
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (_hasHit && (leftDown || leftDrag) && painting)
        {
            PaintAt(_hit.point, _hit.normal);
            e.Use(); SceneView.RepaintAll();
        }
        else if (_hasHit && (leftDown || leftDrag) && erasing)
        {
            EraseAt(_hit.point);
            e.Use(); SceneView.RepaintAll();
        }
    }

    void PaintAt(Vector3 center, Vector3 normal)
    {
        if (prefabs == null || prefabs.Count == 0) return;

        // basis in hit plane (для разброса точек кисти)
        Vector3 n = normal.normalized;
        Vector3 t = Vector3.Cross(n, Vector3.up);
        if (t.sqrMagnitude < 1e-6f) t = Vector3.Cross(n, Vector3.right);
        t.Normalize();
        Vector3 b = Vector3.Cross(n, t);

        // Быстрый список детей parent (spacing без OverlapSphere)
        _tmpChildren.Clear();
        if (parent)
        {
            for (int i = 0; i < parent.childCount; i++)
                _tmpChildren.Add(parent.GetChild(i));
        }
        float minSpacingSqr = minSpacing * minSpacing;

        // Undo-группа на весь мазок
        Undo.SetCurrentGroupName("Paint Vegetation");
        int undoGroup = Undo.GetCurrentGroup();

        int placed = 0, guard = 0;
        int maxAttempts = Mathf.Max(countPerStroke * 10, 200);

        while (placed < countPerStroke && guard++ < maxAttempts)
        {
            // random point in disc
            float r = brushRadius * Mathf.Sqrt(Random.value);
            float theta = Random.value * Mathf.PI * 2f;
            Vector3 offset = t * (Mathf.Cos(theta) * r) + b * (Mathf.Sin(theta) * r);

            if (!TryHitAt(center, n, offset, out var hit))
                continue;

            // двухсторонний уклон: 0..90
            float upAngle = Vector3.Angle(hit.normal, Vector3.up);
            float slope   = Mathf.Min(upAngle, 180f - upAngle);
            if (slope > maxSlope) continue;

            // spacing только против детей parent
            if (avoidOverlap && parent)
            {
                bool blocked = false;
                for (int i = 0; i < _tmpChildren.Count; i++)
                {
                    var tchild = _tmpChildren[i];
                    if (!tchild) continue;

                    Vector3 p = tchild.position;
                    var rend = tchild.GetComponentInChildren<Renderer>();
                    if (rend) p = rend.bounds.ClosestPoint(hit.point);

                    if ((p - hit.point).sqrMagnitude <= minSpacingSqr)
                    { blocked = true; break; }
                }
                if (blocked) continue;
            }

            // prefab
            var prefab = prefabs[Random.Range(0, prefabs.Count)];
            if (!prefab) continue;

            // instantiate — сразу под parent
            GameObject go = parent
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent)
                : (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            Undo.RegisterCreatedObjectUndo(go, "Paint Vegetation");

            // position
            go.transform.position = hit.point;

            // rotation
            if (!preserveRotation)
            {
                Quaternion rot = go.transform.rotation; // prefab base
                if (alignToNormal)
                    rot = Quaternion.FromToRotation(Vector3.up, hit.normal) * rot;

                if (randomYaw)
                {
                    Vector3 yawAxis = alignToNormal ? hit.normal : Vector3.up;
                    rot = Quaternion.AngleAxis(Random.Range(0f, 360f), yawAxis) * rot;
                }

                go.transform.rotation = rot;
            }

            // scale
            float s = Random.Range(Mathf.Min(uniformScale.x, uniformScale.y),
                                   Mathf.Max(uniformScale.x, uniformScale.y));
            go.transform.localScale = Vector3.Scale(go.transform.localScale, new Vector3(s, s, s));

            placed++;
        }

        if (placed > 0) Undo.CollapseUndoOperations(undoGroup);
    }

    // Пытаемся «достучаться» до поверхности из разных направлений
    bool TryHitAt(Vector3 center, Vector3 brushNormal, Vector3 offset, out RaycastHit hit)
    {
        float baseUp = Mathf.Max(brushRadius, 2f) + 5f;  // запас над точкой
        float longUp = Mathf.Max(brushRadius * 4f, 20f); // для глубоких ям
        Vector3 p = center + offset;

        // кандидаты на трассировку (origin, dir, dist), порядок зависит от режима
        var list = new List<(Vector3 o, Vector3 d, float dist)>(6);

        void Add(Vector3 o, Vector3 d, float dist)
        {
            // нормализуем направление для надёжной дистанции
            list.Add((o, d.normalized, dist));
        }

        switch (rayMode)
        {
            case RayDirectionMode.SurfaceNormal:
                Add(p + brushNormal * baseUp, -brushNormal, baseUp * 2f + 5f);
                break;

            case RayDirectionMode.WorldDown:
                Add(p + Vector3.up * longUp, Vector3.down, longUp * 2f + 10f);
                Add(p - Vector3.up * longUp, Vector3.up,    longUp * 2f + 10f);
                break;

            case RayDirectionMode.Camera:
            {
                var cam = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView.camera : null;
                Vector3 fwd = cam ? cam.transform.forward : (SceneView.currentDrawingSceneView ? SceneView.currentDrawingSceneView.camera.transform.forward : Vector3.down);
                float along = longUp;
                Add(p - fwd * along,  fwd, along * 2f + 10f);
                Add(p + fwd * along, -fwd, along * 2f + 10f);
                break;
            }

            case RayDirectionMode.Auto:
            default:
            {
                // 1) по нормали кисти
                Add(p + brushNormal * baseUp, -brushNormal, baseUp * 2f + 5f);
                // 2) вниз по миру (для ям/крутых склонов)
                Add(p + Vector3.up * longUp, Vector3.down, longUp * 2f + 10f);
                // 3) вверх по миру (если красим нижние стороны)
                Add(p - Vector3.up * longUp, Vector3.up,    longUp * 2f + 10f);
                // 4) вдоль камеры (на случай сложных навесов)
                var cam = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView.camera : null;
                if (cam)
                {
                    Vector3 fwd = cam.transform.forward;
                    float along = longUp;
                    Add(p - fwd * along,  fwd, along * 2f + 10f);
                    Add(p + fwd * along, -fwd, along * 2f + 10f);
                }
                break;
            }
        }

        for (int i = 0; i < list.Count; i++)
        {
            var (o, d, dist) = list[i];
            if (Physics.Raycast(o, d, out hit, dist, paintMask, QueryTriggerInteraction.Ignore))
                return true;
        }

        hit = default;
        return false;
    }

    void EraseAt(Vector3 center)
    {
        float r2 = brushRadius * brushRadius;

        Undo.SetCurrentGroupName("Erase Vegetation");
        int undoGroup = Undo.GetCurrentGroup();
        int deleted = 0;

        if (parent)
        {
            var children = new List<Transform>(parent.childCount);
            for (int i = 0; i < parent.childCount; i++)
                children.Add(parent.GetChild(i));

            foreach (var t in children)
            {
                if (!t) continue;
                Vector3 p = t.position;
                var rend = t.GetComponentInChildren<Renderer>();
                if (rend) p = rend.bounds.ClosestPoint(center);
                if ((p - center).sqrMagnitude <= r2)
                {
                    Undo.DestroyObjectImmediate(t.gameObject);
                    deleted++;
                }
            }
        }
        else
        {
            var cols = Physics.OverlapSphere(center, brushRadius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var c in cols)
            {
                if (!c) continue;
                Undo.DestroyObjectImmediate(c.transform.root.gameObject);
                deleted++;
            }
        }

        if (deleted > 0) Undo.CollapseUndoOperations(undoGroup);
    }
}
#endif
