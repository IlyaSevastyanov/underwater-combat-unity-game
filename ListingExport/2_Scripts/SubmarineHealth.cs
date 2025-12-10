using UnityEngine;
using UnityEngine.Events;
using System; // для Action

public class SubmarineHealth : MonoBehaviour
{
    // Событие "получен урон" (amountApplied, current, max)
    public event Action<float, float, float> OnDamaged;

    [Header("Параметры прочности корпуса")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Иммунитет между ударами")]
    public float damageCooldown = 0.5f; // секунды между получениями урона от стен
    private float lastHitTime = -999f;

    [Header("События")]
    public UnityEvent onDeath; // можно назначить через инспектор (перезапуск, сообщение, и т.д.)

    void Awake()
    {
        currentHealth = maxHealth;
    }

    // публично, чтобы другие скрипты могли нанести урон
    public void ApplyDamage(float dmg)
    {
        if (dmg <= 0f) return;

        // защита от слишком частых ударов
        if (Time.time - lastHitTime < damageCooldown)
            return;

        lastHitTime = Time.time;

        float prev = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - dmg);
        float applied = Mathf.Max(0f, prev - currentHealth); // фактически списанный урон

        // ВАЖНО: событие вызываем здесь, внутри метода
        OnDamaged?.Invoke(applied, currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Debug.Log("Субмарина уничтожена (корпус раздавлен).");
            onDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public float GetHealth01() => maxHealth > 0f ? currentHealth / maxHealth : 0f;
}
