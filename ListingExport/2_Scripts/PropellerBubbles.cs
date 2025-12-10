using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PropellerBubbles : MonoBehaviour
{
    [Range(0f, 1f)]
    public float throttle; // получаем отсюда мощность (0..1)

    public float minRate = 0f;    // минимальный поток пузырьков
    public float maxRate = 60f;   // максимальный поток пузырьков при полном газе

    ParticleSystem ps;
    ParticleSystem.EmissionModule emission;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        emission = ps.emission;
    }

    void Update()
    {
        float rate = Mathf.Lerp(minRate, maxRate, throttle);
        emission.rateOverTime = rate;
    }

    public void SetThrottle(float t)
    {
        throttle = Mathf.Clamp01(t);
    }
}
