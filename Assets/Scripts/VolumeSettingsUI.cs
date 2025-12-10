using UnityEngine;
using UnityEngine.UI;

public class VolumeSettingsUI : MonoBehaviour
{
    [Header("Sliders (0..1)")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider engineSlider;
    public Slider sfxSlider;
    public Slider sonarSlider;

    [Header("Buttons")]
    public Button saveButton;
    public Button cancelButton;

    struct VolState
    {
        public float master, music, engine, sfx, sonar;
    }

    VolState initial;

    void Awake()
    {
        // начальные значения из PlayerPrefs
        if (masterSlider) masterSlider.value = SoundSettings.GetMaster();
        if (musicSlider) musicSlider.value = SoundSettings.GetMusic();
        if (engineSlider) engineSlider.value = SoundSettings.GetEngine();
        if (sfxSlider) sfxSlider.value = SoundSettings.GetSfx();
        if (sonarSlider) sonarSlider.value = SoundSettings.GetSonar();

        initial = CaptureCurrent();

        if (saveButton)
        {
            saveButton.interactable = false;
            saveButton.onClick.AddListener(Save);
        }

        if (cancelButton)
            cancelButton.onClick.AddListener(Cancel);

        // слушаем изменения слайдеров
        if (masterSlider) masterSlider.onValueChanged.AddListener(_ => OnChanged());
        if (musicSlider) musicSlider.onValueChanged.AddListener(_ => OnChanged());
        if (engineSlider) engineSlider.onValueChanged.AddListener(_ => OnChanged());
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(_ => OnChanged());
        if (sonarSlider) sonarSlider.onValueChanged.AddListener(_ => OnChanged());
    }

    VolState CaptureCurrent()
    {
        VolState s;
        s.master = masterSlider ? masterSlider.value : SoundSettings.GetMaster();
        s.music = musicSlider ? musicSlider.value : SoundSettings.GetMusic();
        s.engine = engineSlider ? engineSlider.value : SoundSettings.GetEngine();
        s.sfx = sfxSlider ? sfxSlider.value : SoundSettings.GetSfx();
        s.sonar = sonarSlider ? sonarSlider.value : SoundSettings.GetSonar();
        return s;
    }

    bool IsDifferentFromInitial()
    {
        VolState cur = CaptureCurrent();
        const float EPS = 0.0001f;
        if (Mathf.Abs(cur.master - initial.master) > EPS) return true;
        if (Mathf.Abs(cur.music - initial.music) > EPS) return true;
        if (Mathf.Abs(cur.engine - initial.engine) > EPS) return true;
        if (Mathf.Abs(cur.sfx - initial.sfx) > EPS) return true;
        if (Mathf.Abs(cur.sonar - initial.sonar) > EPS) return true;
        return false;
    }

    void OnChanged()
    {
        if (saveButton)
            saveButton.interactable = IsDifferentFromInitial();
    }

    public void Save()
    {
        if (masterSlider) SoundSettings.SetMaster(masterSlider.value);
        if (musicSlider) SoundSettings.SetMusic(musicSlider.value);
        if (engineSlider) SoundSettings.SetEngine(engineSlider.value);
        if (sfxSlider) SoundSettings.SetSfx(sfxSlider.value);
        if (sonarSlider) SoundSettings.SetSonar(sonarSlider.value);

        initial = CaptureCurrent();
        if (saveButton) saveButton.interactable = false;

        // применяем ко всем SubmarineAudio в сцене
        var allAudio = FindObjectsOfType<SubmarineAudio>();
        foreach (var a in allAudio)
        {
            a.ApplyVolumeSettings();
        }
    }

    public void Cancel()
    {
        if (masterSlider) masterSlider.value = initial.master;
        if (musicSlider) musicSlider.value = initial.music;
        if (engineSlider) engineSlider.value = initial.engine;
        if (sfxSlider) sfxSlider.value = initial.sfx;
        if (sonarSlider) sonarSlider.value = initial.sonar;

        if (saveButton) saveButton.interactable = false;
    }
}
