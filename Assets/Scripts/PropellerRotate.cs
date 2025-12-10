using UnityEngine;

public class PropellerRotate : MonoBehaviour
{
    [Header("Speed control")]
    public float maxSpinSpeed = 1200f;
    public float accel = 5f;
    public float decel = 3f;

    [Range(0f, 1f)]
    public float throttleInput = 0f;

    [Header("Axis")]
    public Vector3 localAxis = Vector3.forward;

    [Header("Bubble emitter (prefab)")]
    [Tooltip("ѕ–≈‘јЅ ParticleSystem, который будет создан как ребЄнок пропеллера.")]
    public ParticleSystem bubbleEmitterPrefab;

    [Tooltip("Ёмисси€ при холостом.")]
    public float minEmissionRate = 0f;
    [Tooltip("Ёмисси€ при полном газе.")]
    public float maxEmissionRate = 200f;

    // –≈јЋ№Ќџ… инстанс в сцене
    private ParticleSystem bubbleEmitterInstance;

    private float currentSpinSpeed = 0f;

    private void Start()
    {
        // если есть префаб Ч инстанцируем его под пропеллером
        if (bubbleEmitterPrefab != null)
        {
            bubbleEmitterInstance = Instantiate(
                bubbleEmitterPrefab,
                transform.position,
                transform.rotation,
                transform // делаем дочерним пропеллеру
            );

            var emission = bubbleEmitterInstance.emission;
            emission.enabled = true;
            if (!bubbleEmitterInstance.isPlaying)
                bubbleEmitterInstance.Play();
        }
    }

    void Update()
    {
        float targetSpeed = throttleInput * maxSpinSpeed;

        if (currentSpinSpeed < targetSpeed)
        {
            currentSpinSpeed = Mathf.Lerp(
                currentSpinSpeed,
                targetSpeed,
                accel * Time.deltaTime
            );
        }
        else
        {
            currentSpinSpeed = Mathf.Lerp(
                currentSpinSpeed,
                targetSpeed,
                decel * Time.deltaTime
            );
        }

        transform.Rotate(localAxis, currentSpinSpeed * Time.deltaTime, Space.Self);

        UpdateBubbles();
    }

    private void UpdateBubbles()
    {
        if (bubbleEmitterInstance == null) return;

        float t = 0f;
        if (maxSpinSpeed > 0f)
            t = Mathf.Clamp01(Mathf.Abs(currentSpinSpeed) / maxSpinSpeed);

        var emission = bubbleEmitterInstance.emission;
        emission.enabled = t > 0.01f;
        emission.rateOverTime = Mathf.Lerp(minEmissionRate, maxEmissionRate, t);

        if (emission.enabled)
        {
            if (!bubbleEmitterInstance.isPlaying)
                bubbleEmitterInstance.Play();
        }
        else
        {
            if (bubbleEmitterInstance.isPlaying)
                bubbleEmitterInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void SetThrottle(float t)
    {
        throttleInput = Mathf.Clamp01(t);
    }
}
