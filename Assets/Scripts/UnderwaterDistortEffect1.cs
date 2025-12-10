using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class UnderwaterDistortEffect : MonoBehaviour
{
    public Shader distortShader;
    public float strength = 0.002f;
    public float speed = 1.0f;
    public float frequency = 4.0f;

    Material _mat;

    void OnEnable()
    {
        if (distortShader == null)
            distortShader = Shader.Find("Hidden/UnderwaterDistort_BuiltIn");

        if (distortShader != null)
            _mat = new Material(distortShader);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (_mat == null)
        {
            Graphics.Blit(src, dst);
            return;
        }

        _mat.SetFloat("_Strength", strength);
        _mat.SetFloat("_Speed", speed);
        _mat.SetFloat("_Frequency", frequency);
        _mat.SetFloat("_TimeValue", Time.time);

        Graphics.Blit(src, dst, _mat);
    }
}
