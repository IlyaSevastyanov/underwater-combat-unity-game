using UnityEngine;
using System.Collections;

public class SubmarineAudio : MonoBehaviour
{
    [Header("Одноразовые звуки (PlayOneShot)")]
    [Tooltip("Источник SFX. AudioSource без Loop, без PlayOnAwake.")]
    public AudioSource sfxSource;

    [Header("Длительный звук сонара")]
    [Tooltip("Отдельный AudioSource для сигнала сонара (мы включаем/выключаем через код).")]
    public AudioSource sonarSource;

    [Header("Звук двигателя")]
    [Tooltip("Отдельный AudioSource для звука двигателя (с Loop = true).")]
    public AudioSource engineSource;

    [Tooltip("Минимальный питч двигателя при холостом ходе.")]
    public float engineMinPitch = 0.8f;
    [Tooltip("Максимальный питч двигателя при полной нагрузке.")]
    public float engineMaxPitch = 1.4f;

    [Tooltip("Минимальная громкость двигателя (до умножения на настройки пользователя).")]
    public float engineMinVolume = 0.2f;
    [Tooltip("Максимальная громкость двигателя (до умножения на настройки пользователя).")]
    public float engineMaxVolume = 1.0f;

    [Header("Фоновая музыка")]
    [Tooltip("Отдельный AudioSource для фоновой музыки.")]
    public AudioSource musicSource;
    [Tooltip("Основной музыкальный трек (лууп).")]
    public AudioClip mainMusicLoop;
    [Tooltip("Базовая громкость музыки (до умножения на настройки пользователя).")]
    [Range(0f, 1f)]
    public float baseMusicVolume = 0.5f;

    [Header("Базовые громкости каналов (множители)")]
    [Range(0f, 1f)] public float baseSfxVolume = 1.0f;
    [Range(0f, 1f)] public float baseSonarVolume = 0.5f;

    [Header("Клипы")]
    [Tooltip("Звук сонара/сканирования.")]
    public AudioClip sonarPing;

    [Tooltip("Глухой удар корпуса (столкновение с дном/камнем).")]
    public AudioClip hullHit;

    [Tooltip("Аварийный звук при выходе за рамки глубины/высоты.")]
    public AudioClip boundaryHit;

    [Tooltip("Позитивный звук: поймал хорошую рыбу / получил очки.")]
    public AudioClip scoreGain;

    [Tooltip("Звук плохой рыбы: поймал опасную / ядовитую / штрафную.")]
    public AudioClip hostileFishClip;

    [Header("Настройки сонара")]
    [Tooltip("Сколько секунд держится звук сонара после нажатия.")]
    public float sonarDuration = 2f;

    [Tooltip("Базовая громкость звука сонара (до умножения на настройки пользователя).")]
    public float sonarVolume = 0.5f;

    private Coroutine sonarRoutine;

    void Start()
    {
        // --- ДВИГАТЕЛЬ ---
        if (engineSource != null)
        {
            engineSource.loop = true;
            if (!engineSource.isPlaying)
            {
                engineSource.Play();
            }

            // Стартовые значения громкости/питча. Громкость позже будет корректироваться настройками.
            engineSource.pitch = engineMinPitch;
            engineSource.volume = engineMinVolume * GetEngineFactor();
        }

        // --- МУЗЫКА ---
        if (musicSource != null && mainMusicLoop != null)
        {
            musicSource.clip = mainMusicLoop;
            musicSource.loop = true;
            musicSource.volume = baseMusicVolume * GetMusicFactor();
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        // На старте синхронизируем всё с текущими настройками
        ApplyVolumeSettings();
    }

    // === ВСПОМОГАТЕЛЬНЫЕ ФАКТОРЫ ===

    float GetMasterFactor() => SoundSettings.GetMaster();

    float GetMusicFactor() => SoundSettings.GetMusic() * GetMasterFactor();
    float GetEngineFactor() => SoundSettings.GetEngine() * GetMasterFactor();
    float GetSfxFactor() => SoundSettings.GetSfx() * GetMasterFactor();
    float GetSonarFactor() => SoundSettings.GetSonar() * GetMasterFactor();

    // Можно вызывать из UI настроек после изменения слайдеров
    public void ApplyVolumeSettings()
    {
        // Музыка
        if (musicSource != null)
        {
            musicSource.volume = baseMusicVolume * GetMusicFactor();
        }

        // Если сонар уже играет — обновим громкость
        if (sonarSource != null && sonarSource.isPlaying)
        {
            sonarSource.volume = sonarVolume * baseSonarVolume * GetSonarFactor();
        }

        // Двигатель: громкость будет пересчитана в SetEngineLoad,
        // но можно слегка обновить айдл-состояние
        if (engineSource != null)
        {
            // Здесь ставим громкость "холостого хода", а дальше при движении SetEngineLoad всё подправит
            engineSource.volume = engineMinVolume * GetEngineFactor();
        }

        // SFX будет учитывать фактор при PlayOneShot
    }

    // === ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ ДРУГИХ СКРИПТОВ ===

    // ОБНОВЛЕНИЕ ЗВУКА ДВИГАТЕЛЯ ИЗ КОНТРОЛЛЕРА ПОДЛОДКИ
    // вызывается из SubmarineController.UpdatePropellersAndAudio()
    public void SetEngineLoad(float throttle, float speed)
    {
        if (engineSource == null) return;

        // нормализуем скорость условно до 0..1 (подстрой maxSpeed под свою игру)
        float maxSpeedForSound = 10f;
        float speedNorm = Mathf.Clamp01(speed / maxSpeedForSound);

        // комбинируем газ и скорость в некий "load" 0..1
        float load = Mathf.Clamp01(0.6f * throttle + 0.4f * speedNorm);

        float baseVol = Mathf.Lerp(engineMinVolume, engineMaxVolume, load);
        engineSource.pitch = Mathf.Lerp(engineMinPitch, engineMaxPitch, load);
        engineSource.volume = baseVol * GetEngineFactor();
    }

    // Включаем сонар по кнопке
    public void TriggerSonar()
    {
        if (sonarRoutine == null)
        {
            sonarRoutine = StartCoroutine(SonarRoutine());
        }
    }

    IEnumerator SonarRoutine()
    {
        if (sonarSource && sonarPing)
        {
            sonarSource.clip = sonarPing;
            sonarSource.volume = sonarVolume * baseSonarVolume * GetSonarFactor();
            sonarSource.loop = true;
            sonarSource.Play();
        }

        yield return new WaitForSeconds(sonarDuration);

        if (sonarSource)
        {
            sonarSource.Stop();
            sonarSource.loop = false;
            sonarSource.clip = null;
        }

        sonarRoutine = null;
    }

    // Столкновение с твёрдым объектом — треск корпуса
    public void PlayHullHit()
    {
        if (sfxSource && hullHit)
        {
            // множитель громкости SFX-канала
            float v = 1f * baseSfxVolume * GetSfxFactor();
            sfxSource.PlayOneShot(hullHit, v);
        }
    }

    // Вылетел за границы глубины — тревожный сигнал
    public void PlayBoundaryHit()
    {
        if (sfxSource && boundaryHit)
        {
            float v = 0.8f * baseSfxVolume * GetSfxFactor();
            sfxSource.PlayOneShot(boundaryHit, v);
        }
    }

    // Хорошая рыба / награда / плюс очки
    public void PlayScoreGain()
    {
        if (sfxSource && scoreGain)
        {
            float v = 0.7f * baseSfxVolume * GetSfxFactor();
            sfxSource.PlayOneShot(scoreGain, v);
        }
    }

    // Плохая рыба / штраф / яд / опасность
    public void PlayHostileFish()
    {
        float v = 1f * baseSfxVolume * GetSfxFactor();

        // 1. основной звук плохой рыбы
        if (sfxSource && hostileFishClip)
        {
            sfxSource.PlayOneShot(hostileFishClip, v);
            return;
        }

        // 2. fallback, если вдруг забыли назначить hostileFishClip
        if (sfxSource && hullHit)
        {
            sfxSource.PlayOneShot(hullHit, v);
        }
    }
    // Глобальное включение/выключение звука для всех каналов этого SubmarineAudio
    public void ApplySoundEnabled(bool enabled)
    {
        bool mute = !enabled;

        // Фоновая музыка
        if (musicSource != null)
            musicSource.mute = mute;

        // Двигатель
        if (engineSource != null)
            engineSource.mute = mute;

        // Обычные SFX
        if (sfxSource != null)
            sfxSource.mute = mute;

        // Сонар
        if (sonarSource != null)
        {
            sonarSource.mute = mute;

            // если вырубаем звук и сонар сейчас играет — сразу остановим
            if (mute && sonarSource.isPlaying)
            {
                sonarSource.Stop();
                sonarSource.loop = false;
                sonarSource.clip = null;
            }
        }
    }
}
