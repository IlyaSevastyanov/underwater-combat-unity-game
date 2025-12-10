using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SubmarineCollisionDamage : MonoBehaviour
{
    [Header("Урон при столкновении")]
    public float minImpactSpeed = 2f;   // скорость удара, порог
    public float damagePerHit = 10f;    // урон за удар

    [Header("Слои, которые наносят урон")]
    public LayerMask damagingLayers = ~0;
    // По умолчанию: все слои. В инспекторе выбери только стены/пол итд.

    private SubmarineHealth health;
    private SubmarineAudio audioRef;

    void Awake()
    {
        health = GetComponent<SubmarineHealth>();
        if (health == null)
        {
            Debug.LogWarning("[SubmarineCollisionDamage] Нет SubmarineHealth!");
        }

        audioRef = GetComponent<SubmarineAudio>();
        if (audioRef == null)
        {
            Debug.LogWarning("[SubmarineCollisionDamage] Нет SubmarineAudio!");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (health == null) return;

        // --- ПРОВЕРКА СЛОЯ ---
        int otherLayer = collision.gameObject.layer;
        if ((damagingLayers.value & (1 << otherLayer)) == 0)
        {
            // слой не входит в маску damagingLayers — игнорируем столкновение для урона
            return;
        }

        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed >= minImpactSpeed)
        {
            health.ApplyDamage(damagePerHit);

#if UNITY_EDITOR
            Debug.Log("Удар о " + collision.collider.name + " : -" + damagePerHit + " HP");
#endif

            if (audioRef != null)
            {
                audioRef.PlayHullHit();
            }
        }
    }

    // Если хочешь урон при "трении", можешь добавить это:
    /*
    void OnCollisionStay(Collision collision)
    {
        if (health == null) return;

        int otherLayer = collision.gameObject.layer;
        if ((damagingLayers.value & (1 << otherLayer)) == 0)
            return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed >= minImpactSpeed)
        {
            health.ApplyDamage(damagePerHit * Time.deltaTime);
        }
    }
    */
}
