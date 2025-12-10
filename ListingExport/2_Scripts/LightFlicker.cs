using UnityEngine;
public class LightFlicker : MonoBehaviour
{
    public Light targetLight;
    public float baseIntensity = 3f;
    public float flickerAmount = 0.2f;
    public float flickerSpeed = 2f;

    void Update()
    {
        if (!targetLight) return;

        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        targetLight.intensity = baseIntensity + (noise - 0.5f) * flickerAmount;
    }
}
