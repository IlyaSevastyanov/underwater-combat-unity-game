using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class HintsToggleWithSave : MonoBehaviour
{
    [Header("UI")]
    public Toggle toggle;          // твой Toggle "¬ключить подсказки?"
    public Button saveButton;      // кнопка "—ќ’–јЌ»“№" (изначально неактивна)
    public Button cancelButton;    // (опц.) "ќ“ћ≈Ќ»“№"

    bool initialValue;

    void Awake()
    {
        toggle ??= GetComponent<Toggle>();

        // загрузили сохранЄнное значение
        initialValue = HintsPref.Get();
        toggle.isOn = initialValue;

        if (saveButton) { saveButton.interactable = false; saveButton.onClick.AddListener(Save); }
        if (cancelButton) cancelButton.onClick.AddListener(Cancel);

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
        if (saveButton) saveButton.interactable = (toggle.isOn != initialValue);
    }

    public void Save()
    {
        HintsPref.Set(toggle.isOn);
        initialValue = toggle.isOn;
        if (saveButton) saveButton.interactable = false;
    }

    public void Cancel()
    {
        toggle.isOn = initialValue;
        if (saveButton) saveButton.interactable = false;
    }
}
