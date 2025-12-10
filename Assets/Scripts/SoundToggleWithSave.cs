using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class SoundToggleWithSave : MonoBehaviour
{
    [Header("UI")]
    public Toggle toggle;      // Toggle "«вук"
    public Button saveButton;  // "—ќ’–јЌ»“№"
    public Button cancelButton; // "ќ“ћ≈Ќ»“№" (опционально)

    bool initialValue;

    void Awake()
    {
        toggle ??= GetComponent<Toggle>();

        // загрузили сохранЄнное значение
        initialValue = SoundPref.Get();
        toggle.isOn = initialValue;

        if (saveButton)
        {
            saveButton.interactable = false;
            saveButton.onClick.AddListener(Save);
        }

        if (cancelButton)
            cancelButton.onClick.AddListener(Cancel);

        toggle.onValueChanged.AddListener(_ => OnChanged());
    }

    void OnDestroy()
    {
        toggle.onValueChanged.RemoveAllListeners();
        if (saveButton) saveButton.onClick.RemoveAllListeners();
        if (cancelButton) cancelButton.onClick.RemoveAllListeners();
    }

    void OnChanged()
    {
        if (saveButton)
            saveButton.interactable = (toggle.isOn != initialValue);
    }

    public void Save()
    {
        SoundPref.Set(toggle.isOn);
        initialValue = toggle.isOn;
        if (saveButton) saveButton.interactable = false;

        // сразу применим ко всем звукам
        ApplyToAllAudio();
    }

    public void Cancel()
    {
        toggle.isOn = initialValue;
        if (saveButton) saveButton.interactable = false;
    }

    void ApplyToAllAudio()
    {
        bool enabled = SoundPref.Get();
        var all = FindObjectsOfType<SubmarineAudio>();
        foreach (var sa in all)
            sa.ApplySoundEnabled(enabled);
    }

}
