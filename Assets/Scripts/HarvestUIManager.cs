using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HarvestUIManager : MonoBehaviour
{
    [Header("Choice Popup (Да/Нет)")]
    public GameObject promptPanel;
    public TextMeshProUGUI promptText;
    public Button yesButton;
    public Button noButton;

    [Header("Cut Choice Popup (Филе / Туша)")]
    public GameObject cutChoicePanel;
    public TextMeshProUGUI cutChoiceText;
    public Button filletButton;
    public Button carcassButton;

    [Header("Info Panel (описание вида)")]
    public GameObject infoPanel;
    public TextMeshProUGUI infoText;
    public Button infoCloseButton;

    // runtime-ссылки на текущий кэпч
    private FishData pendingFish;
    private GameObject pendingFishObj;
    private FishCatcher catcher;

    // был ли вид известен на момент поимки (чтобы лор показать только один раз)
    private bool wasKnownAtCatch = false;

    // только заморозка (подсветку убрали)
    private FishFreezeController freezeCtrl;

    void Start()
    {
        HidePrompt();
        HideCutChoicePanel();
        HideInfoPanel();

        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(OnYes);
        }
        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(OnNo);
        }
        if (filletButton != null)
        {
            filletButton.onClick.RemoveAllListeners();
            filletButton.onClick.AddListener(OnFillet);
        }
        if (carcassButton != null)
        {
            carcassButton.onClick.RemoveAllListeners();
            carcassButton.onClick.AddListener(OnCarcass);
        }
        if (infoCloseButton != null)
        {
            infoCloseButton.onClick.RemoveAllListeners();
            infoCloseButton.onClick.AddListener(HideInfoPanel);
        }

        Debug.Log("[HarvestUI] Start: wiring done");
    }

    // === ПУБЛИЧНЫЙ ВХОД ОТ FishCatcher ===
    // Всегда показываем окно; лор — только при первом знакомстве
    public void ShowPrompt(FishData fish, GameObject fishObj, FishCatcher whoCalled)
    {
        pendingFish = fish;
        pendingFishObj = fishObj;
        catcher = whoCalled;

        wasKnownAtCatch = false;
        if (FishKnowledge.Instance != null && fish != null)
            wasKnownAtCatch = FishKnowledge.Instance.IsKnown(fish.fishName);

        // Показать вопрос
        if (promptPanel != null) promptPanel.SetActive(true);
        if (promptText != null && fish != null)
            promptText.text = $"Освежевать водного обитателя?\n{fish.fishName}";

        HideInfoPanel();
        HideCutChoicePanel();

        // Сразу стопаем рыбу, чтобы не уплывала во время выбора
        FreezeFish();

        // Ставим игру на паузу
        Time.timeScale = 0f;

        // В режиме шутера при таком popup курсор нужен
        SetCursorForUI(true);

        Debug.Log("[HarvestUI] Catch: " + (fish != null ? fish.fishName : "null") +
                  " → показать выбор (always), wasKnown=" + wasKnownAtCatch);
    }

    // === КНОПКИ "ДА"/"НЕТ" И ВТОРОЙ ВЫБОР ===============================

    // Игрок нажал "ДА" -> показываем модалку Филе/Туша
    void OnYes()
    {
        Debug.Log("[HarvestUI] YES clicked. pendingFish=" + (pendingFish != null ? pendingFish.fishName : "null"));

        HidePrompt();

        if (cutChoicePanel != null) cutChoicePanel.SetActive(true);
        if (cutChoiceText != null) cutChoiceText.text = "Желаете получить филе или тушу?";
        // Пауза остаётся; рыба заморожена контроллером
        // Курсор уже включён в ShowPrompt()
    }

    // Игрок нажал "НЕТ"
    void OnNo()
    {
        Debug.Log("[HarvestUI] NO clicked. pendingFish=" + (pendingFish != null ? pendingFish.fishName : "null"));

        if (catcher != null && pendingFish != null)
        {
            // butcher=false → рыба просто исчезает, без урона/пользы
            catcher.ResolveCatch(pendingFish, pendingFishObj, false);
        }

        ClearPending(); // снимаем всё и резюмим время
    }

    // === Игрок выбрал "Филе" ====================================
    void OnFillet()
    {
        Debug.Log("[HarvestUI] FILLET clicked. pendingFish=" + (pendingFish != null ? pendingFish.fishName : "null"));

        if (catcher != null && pendingFish != null && pendingFishObj != null)
        {
            var seq = pendingFishObj.GetComponent<ButcherSequencer>();

            // Снимаем паузу, чтобы пошёл секвенсер
            Time.timeScale = 1f;
            if (cutChoicePanel != null) cutChoicePanel.SetActive(false);

            // курсор пока оставляем видимым, игрок только что кликнул по UI,
            // а дальше может показаться панель лора

            // локальные копии для колбэка
            var fish = pendingFish;
            var fishGO = pendingFishObj;
            var fc = catcher;
            bool showLoreAtEnd = (FishKnowledge.Instance != null) ? !FishKnowledge.Instance.IsKnown(fish.fishName) : false;

            Action finish = () =>
            {
                // очки/здоровье и т.д.
                fc.ResolveCatch(fish, fishGO, true);

                if (FishKnowledge.Instance != null)
                    FishKnowledge.Instance.MarkKnown(fish.fishName);

                // 🔸 вот тут показываем hint через HintsPopupUI
                string resultText = $"Вы заполучили филе {fish.fishName}";
                ShowHarvestHint(resultText);

                // лор — отдельно, только описание вида
                if (showLoreAtEnd && !string.IsNullOrEmpty(fish.description))
                    ShowInfoPanelWithText(fish.description);

                ClearPending(); // размораживаем и очищаем (курсор спрячется, если лора нет)
            };

            if (seq != null)
            {
                // важно: чтобы секвенсер знал, кого разделывает
                seq.Setup(fc, fish, fishGO);
                seq.StartButchering(HarvestMode.Fillet, finish);
            }
            else
            {
                finish();
            }
        }
    }

    // === Игрок выбрал "Туша" ====================================
    void OnCarcass()
    {
        Debug.Log("[HarvestUI] CARCASS clicked. pendingFish=" + (pendingFish != null ? pendingFish.fishName : "null"));

        if (catcher != null && pendingFish != null && pendingFishObj != null)
        {
            var seq = pendingFishObj.GetComponent<ButcherSequencer>();

            // Снимаем паузу для последовательности разделки
            Time.timeScale = 1f;
            if (cutChoicePanel != null) cutChoicePanel.SetActive(false);

            var fish = pendingFish;
            var fishGO = pendingFishObj;
            var fc = catcher;
            bool showLoreAtEnd = (FishKnowledge.Instance != null) ? !FishKnowledge.Instance.IsKnown(fish.fishName) : false;

            Action finish = () =>
            {
                fc.ResolveCatch(fish, fishGO, true);

                if (FishKnowledge.Instance != null)
                    FishKnowledge.Instance.MarkKnown(fish.fishName);

                // 🔸 hint для туши
                string resultText = $"Вы заполучили тушу {fish.fishName}";
                ShowHarvestHint(resultText);

                // лор — если первый раз
                if (showLoreAtEnd && !string.IsNullOrEmpty(fish.description))
                    ShowInfoPanelWithText(fish.description);

                ClearPending();
            };

            if (seq != null)
            {
                seq.Setup(fc, fish, fishGO);
                seq.StartButchering(HarvestMode.Carcass, finish);
            }
            else
            {
                finish();
            }
        }
    }

    void ShowInfoPanelWithText(string body)
    {
        if (infoPanel != null) infoPanel.SetActive(true);
        if (infoText != null) infoText.text = body;

        // при показе лора курсор тоже нужен
        SetCursorForUI(true);

        Debug.Log("[HarvestUI] ShowInfoPanel (lore): " + body);
    }

    void ShowHarvestHint(string msg)
    {
        if (HintsPopupUI.I != null)
        {
            // Можно передать своё время показа, например 3 секунды
            HintsPopupUI.I.Enqueue(msg, 3f);
        }
        else
        {
            Debug.Log("[HarvestUI] HintsPopupUI.I is null. Msg: " + msg, this);
        }
    }

    void HideInfoPanel()
    {
        if (infoPanel != null) infoPanel.SetActive(false);

        // Если больше нет UI по разделке — возвращаем шутерный режим
        if ((promptPanel == null || !promptPanel.activeSelf) &&
            (cutChoicePanel == null || !cutChoicePanel.activeSelf))
        {
            SetCursorForUI(false);
        }
    }

    // === СЛУЖЕБНЫЕ ХЕЛПЕРЫ ======================================

    void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    void HideCutChoicePanel()
    {
        if (cutChoicePanel != null) cutChoicePanel.SetActive(false);
    }

    // Только фриз/анфриз без подсветки
    void FreezeFish()
    {
        if (!pendingFishObj) return;

        if (!freezeCtrl) freezeCtrl = pendingFishObj.GetComponent<FishFreezeController>();
        if (!freezeCtrl) freezeCtrl = pendingFishObj.AddComponent<FishFreezeController>();

        freezeCtrl.Freeze();

        Debug.Log($"[HarvestUI] Freeze: fish='{pendingFishObj.name}'");
    }

    void UnfreezeFish()
    {
        if (freezeCtrl) freezeCtrl.Unfreeze();
        freezeCtrl = null;
    }

    // Сценарий: была пауза (новая рыба), надо снять паузу и очистить состояние
    void ClearPending()
    {
        // вернуть управление рыбой (если объект ещё жив)
        UnfreezeFish();

        // снять паузу (если стояла)
        Time.timeScale = 1f;

        // скрыть модалки
        if (promptPanel != null) promptPanel.SetActive(false);
        if (cutChoicePanel != null) cutChoicePanel.SetActive(false);

        // очистить ссылки
        pendingFish = null;
        pendingFishObj = null;
        catcher = null;

        // если панель лора НЕ показана — возвращаем шутерный режим
        if (infoPanel == null || !infoPanel.activeSelf)
        {
            SetCursorForUI(false);
        }

        Debug.Log("[HarvestUI] ClearPending -> resume time");
    }

    // Сценарий: рыба уже известна, мы не ставили паузу вообще
    void ClearPending_NoUnpause()
    {
        UnfreezeFish();
        HidePrompt();
        HideCutChoicePanel();

        pendingFish = null;
        pendingFishObj = null;
        catcher = null;

        if (infoPanel == null || !infoPanel.activeSelf)
        {
            SetCursorForUI(false);
        }

        Debug.Log("[HarvestUI] ClearPending_NoUnpause (не было паузы)");
    }

    // --- Управление курсором под шутерный режим ---
    void SetCursorForUI(bool uiActive)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        if (uiActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
#endif
    }
}
