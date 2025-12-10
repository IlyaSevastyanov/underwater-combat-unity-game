using System.Collections.Generic;
using UnityEngine;

public class FishHighlighter : MonoBehaviour
{
    [Header("Shader name")]
    public string rimShaderName = "Hidden/Fish/RimOnly";   // важно!

    [Header("Debug")]
    public bool debugLogs = false;
    public bool forceTestRim = false; // можно вообще не использовать, но пусть будет

    private readonly Dictionary<Renderer, Material> added = new Dictionary<Renderer, Material>();

    public void EnableHighlight(Color color, float intensity = 4f, float rimPower = 0.02f)
    {
        DisableHighlight();

        var rendList = GetComponentsInChildren<Renderer>(true);
        Shader rimShader = Shader.Find(rimShaderName);
        if (!rimShader)
        {
            Debug.LogError("[FishHighlighter] Rim shader not found: " + rimShaderName, this);
            return;
        }

        int addedCount = 0;

        foreach (var r in rendList)
        {
            if (!r || !r.enabled) continue;

            var mats = r.sharedMaterials;
            var mat = new Material(rimShader);
            mat.hideFlags = HideFlags.DontSave;

            // ИМЕНА СВОЙСТВ СОВПАДАЮТ С ШЕЙДЕРОМ
            mat.SetColor("_Color", color);
            mat.SetFloat("_Intensity", intensity);
            mat.SetFloat("_RimPower", rimPower);   // толщина

            var newArr = new Material[mats.Length + 1];
            for (int i = 0; i < mats.Length; i++) newArr[i] = mats[i];
            newArr[newArr.Length - 1] = mat;
            r.sharedMaterials = newArr;

            added[r] = mat;
            addedCount++;
        }

        if (debugLogs)
            Debug.Log($"[FishHighlighter] EnableHighlight added={addedCount}", this);
    }

    public void DisableHighlight()
    {
        int removed = 0;

        foreach (var kv in added)
        {
            var r = kv.Key;
            var mat = kv.Value;

            if (!r)
            {
                if (mat) Object.Destroy(mat);
                removed++;
                continue;
            }

            var mats = new List<Material>(r.sharedMaterials);
            if (mats.Remove(mat))
            {
                r.sharedMaterials = mats.ToArray();
                removed++;
            }

            if (mat) Object.Destroy(mat);
        }

        added.Clear();

        if (debugLogs)
            Debug.Log($"[FishHighlighter] DisableHighlight removed={removed}", this);
    }
}
