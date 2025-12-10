using UnityEngine;

public class TutorialHintsMobile : MonoBehaviour
{
    public static bool Active { get; private set; }

    [Header("Refs")]
    public SubmarineController subController;

    [Header("Запуск")]
    [Tooltip("Запускать ли мобильный туториал, только если включены подсказки в настройках.")]
    public bool onlyIfHintsEnabled = true;

    // 0 = движение, 1 = вверх/вниз, 2 = скан, 3 = поймать рыбу
    int step;
    bool movedForward, movedBack, movedLeft, movedRight;
    bool movedUp, movedDown;

    void OnEnable()
    {
        FishCatcher.OnAnyFishCaught += OnAnyFishCaught;
    }

    void OnDisable()
    {
        FishCatcher.OnAnyFishCaught -= OnAnyFishCaught;
    }

    void Start()
    {
        // больше НИКАКОЙ проверки платформы

        if (onlyIfHintsEnabled && !HintsPref.Get())
        {
            enabled = false;
            return;
        }

        if (!subController)
        {
            Debug.LogWarning("TutorialHintsMobile: no SubmarineController reference");
            enabled = false;
            return;
        }

        Begin();
    }

    void Begin()
    {
        Active = true;

        // Пауза игры, но SubmarineController продолжает получать input
        Time.timeScale = 0f;

        step = 0;
        movedForward = movedBack = movedLeft = movedRight = movedUp = movedDown = false;

        HintsPopupUI.I?.ShowSticky(
            "Движение: используй левый круглый джойстик — подвигай подлодку во все стороны."
        );
    }

    void Update()
    {
        if (!Active) return;

        switch (step)
        {
            case 0: HandleStep0_Movement(); break;
            case 1: HandleStep1_UpDown(); break;
            case 2: /* ждём кнопку сонара (OnScanPressedMobile) */ break;
            case 3: /* ждём OnAnyFishCaught */ break;
        }
    }

    // ---------- Шаг 0: движение ----------

    void HandleStep0_Movement()
    {
        float move = subController.MoveAxis;
        float yaw = subController.YawAxis;

        const float threshold = 0.4f;

        if (move > threshold) movedForward = true;
        if (move < -threshold) movedBack = true;
        if (yaw < -threshold) movedLeft = true;
        if (yaw > threshold) movedRight = true;

        if (movedForward && movedBack && movedLeft && movedRight)
        {
            step = 1;

            HintsPopupUI.I?.ShowSticky(
                "Подъём/спуск: правый вертикальный рычаг — потяни вверх и вниз."
            );
        }
    }

    // ---------- Шаг 1: вверх/вниз ----------

    void HandleStep1_UpDown()
    {
        float upAxis = subController.UpAxis;
        const float vThreshold = 0.4f;

        if (upAxis > vThreshold) movedUp = true;
        if (upAxis < -vThreshold) movedDown = true;

        if (movedUp && movedDown)
        {
            step = 2;

            HintsPopupUI.I?.ShowSticky(
                "Скан: нажми кнопку сонара на экране."
            );
        }
    }

    // ---------- Шаг 2: скан ----------

    // Вызывается с кнопки сонара (UI Button) на мобилке
    public void OnScanPressedMobile()
    {
        if (!Active || step != 2) return;

        CompleteScanStep();
    }

    void CompleteScanStep()
    {
        step = 3;

        // После скана игра продолжает идти
        Time.timeScale = 1f;

        HintsPopupUI.I?.ShowSticky(
            "Поймай рыбу: подплыви носом к рыбе, дождись окна и выбери действие."
        );
    }

    void OnAnyFishCaught()
    {
        if (!Active || step != 3) return;
        Finish();
    }

    void Finish()
    {
        Active = false;
        HintsPopupUI.I?.HideImmediate();
        enabled = false;
    }
}
