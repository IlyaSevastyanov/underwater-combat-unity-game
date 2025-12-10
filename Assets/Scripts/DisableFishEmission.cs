using UnityEngine;

public class DisableFishEmission : MonoBehaviour
{
    void Awake()
    {
        var rend = GetComponentInChildren<Renderer>();
        if (rend == null) return;

        // создаём инстанс материала только для этой рыбы
        var mat = rend.material;

        if (mat.IsKeywordEnabled("_EMISSION"))
        {
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
        }
    }
}
