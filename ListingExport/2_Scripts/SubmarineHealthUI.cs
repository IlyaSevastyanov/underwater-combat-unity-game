using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SubmarineHealthUI : MonoBehaviour
{
    [Header("Targets")]
    public SubmarineHealth target;            // здоровье субмарины
    public SubmarineController subController; // управление субмариной

    [Header("Health Bar (tiled/sliced)")]
    public RectTransform healthFillRT;        // RectTransform HealthFill
    public Image healthFillImage;             // Image на HealthFill (Tiled или Sliced)
    public float maxWidth = 200f;             // ширина при фулл-хп
    public float barHeight = 24f;             // высота бара

    [Header("Low HP blink")]
    public float lowHealthThreshold = 0.2f;   // ниже этого мигаем
    public float blinkSpeed = 6f;             // скорость мигания
    private float baseAlpha = 1f;             // запоминаем изначальную альфу

    [Header("Death screen")]
    public CanvasGroup deathPanel;            // DeathPanel (CanvasGroup)
    public float deathFadeTime = 0.5f;        // скорость затемнения
    public string mainMenuSceneName = "MainMenu";

    [Header("Death audio")]
    public AudioSource deathAudioSource;
    public AudioClip deathClip;
    public float deathVolume = 1f;

    [Header("Hints")]
    public bool showFirstDamageHint = true;
    public bool showCriticalHint = true;
    [Range(0f, 1f)] public float criticalPct = 0.25f;

    bool isDead = false;
    bool deathSoundPlayed = false;
    float shownFrac = 1f;
    bool firstDamageShown = false; // для первого сообщения об уроне

    void Start()
    {
        // стартовая альфа
        if (healthFillImage != null)
            baseAlpha = healthFillImage.color.a;

        // спрятать экран смерти
        if (deathPanel != null)
        {
            deathPanel.alpha = 0f;
            deathPanel.interactable = false;
            deathPanel.blocksRaycasts = false;
            deathPanel.gameObject.SetActive(false);
        }

        // выставить бар на полный
        if (healthFillRT != null)
        {
            var size = healthFillRT.sizeDelta;
            size.x = maxWidth;
            size.y = barHeight;
            healthFillRT.sizeDelta = size;
        }

        // ПОДПИСКА на событие урона
        if (target != null)
            target.OnDamaged += HandleDamaged;
    }

    void OnDestroy()
    {
        if (target != null)
            target.OnDamaged -= HandleDamaged;
    }

    // Первое сообщение об уроне + критическое состояние
    void HandleDamaged(float amount, float current, float max)
    {
        if (isDead) return;

        if (showFirstDamageHint && !firstDamageShown)
        {
            firstDamageShown = true;
            HintsRuntime.Show("ВНИМАНИЕ: получен урон! Держитесь подальше от опасной рыбы, стен и всплытия", 2.6f, "dmg_first", 999f);
        }

        if (showCriticalHint && current <= max * Mathf.Clamp01(criticalPct))
        {
            HintsRuntime.Show("Критическое состояние корпуса! Избегайте столкновений.", 2.6f, "lowhp", 10f);
        }
    }

    void Update()
    {
        if (target == null || healthFillRT == null || healthFillImage == null)
            return;

        float hpFrac = Mathf.Clamp01(target.currentHealth / target.maxHealth);

        // сглаживание ширины
        shownFrac = Mathf.Lerp(shownFrac, hpFrac, 10f * Time.unscaledDeltaTime);

        var size = healthFillRT.sizeDelta;
        size.x = maxWidth * shownFrac;
        size.y = barHeight;
        healthFillRT.sizeDelta = size;

        // цвет бара
        Color c;
        if (hpFrac > 0.5f)
        {
            c = Color.Lerp(new Color(1f, 0.8f, 0.2f, 0.9f),
                           new Color(0.2f, 1f, 0.4f, 0.9f),
                           Mathf.InverseLerp(0.5f, 1f, hpFrac));
        }
        else
        {
            c = Color.Lerp(new Color(0.8f, 0.0f, 0.0f, 0.9f),
                           new Color(1f, 0.5f, 0.0f, 0.9f),
                           Mathf.InverseLerp(0.0f, 0.5f, hpFrac));
        }

        if (!isDead)
        {
            if (hpFrac <= 0f)
            {
                StartDeath();
                c = new Color(1f, 0f, 0f, 1f);
            }
            else if (hpFrac <= lowHealthThreshold)
            {
                float pulse = 0.5f + 0.5f * Mathf.PingPong(Time.unscaledTime * blinkSpeed, 1f);
                c.a = baseAlpha * pulse;
            }
            else
            {
                c.a = baseAlpha;
            }
        }
        else
        {
            c = new Color(1f, 0f, 0f, 1f);
        }

        healthFillImage.color = c;

        // фейд экрана смерти
        if (isDead && deathPanel != null)
        {
            deathPanel.alpha = Mathf.MoveTowards(deathPanel.alpha, 1f, Time.unscaledDeltaTime / deathFadeTime);
            if (deathPanel.alpha >= 0.99f)
            {
                deathPanel.interactable = true;
                deathPanel.blocksRaycasts = true;
            }
        }
    }

    void StartDeath()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Субмарина уничтожена. Корпус раздавлен.");

        if (subController != null)
            subController.enabled = false;

        Time.timeScale = 0f;

        if (deathPanel != null)
        {
            deathPanel.gameObject.SetActive(true);
            deathPanel.alpha = 0f;
            deathPanel.interactable = false;
            deathPanel.blocksRaycasts = false;
        }

        if (!deathSoundPlayed)
        {
            PlayDeathSound();
            deathSoundPlayed = true;
        }
    }

    void PlayDeathSound()
    {
        if (!deathAudioSource || !deathClip)
        {
            if (!deathAudioSource) Debug.LogWarning("[SubmarineHealthUI] deathAudioSource = NULL.");
            if (!deathClip) Debug.LogWarning("[SubmarineHealthUI] deathClip = NULL.");
            return;
        }

        AudioListener.pause = false; // на всякий случай
        deathAudioSource.volume = deathVolume;
        deathAudioSource.clip = deathClip;
        deathAudioSource.loop = false;
        deathAudioSource.Play();
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
