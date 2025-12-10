using UnityEngine;

public class TutorialHintsPC : MonoBehaviour
{
    public static bool Active { get; private set; }

    [Header("Refs")]
    public SubmarineController subController;

    [Header("Запуск")]
    public bool onlyIfHintsEnabled = true;

    [Header("Клавиши управления")]
    public KeyCode keyForward = KeyCode.W;
    public KeyCode keyBackward = KeyCode.S;
    public KeyCode keyLeft = KeyCode.A;
    public KeyCode keyRight = KeyCode.D;

    public KeyCode keyUp = KeyCode.Space;
    public KeyCode keyDown = KeyCode.R;

    public KeyCode keyScan = KeyCode.E;

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
        if (onlyIfHintsEnabled && !HintsPref.Get())
        {
            enabled = false;
            return;
        }

        if (!subController)
        {
            Debug.LogWarning("TutorialHintsPC: no SubmarineController reference");
            enabled = false;
            return;
        }

        Begin();
    }

    void Begin()
    {
        Active = true;

        // Пауза игры, но ввод читаем
        Time.timeScale = 0f;

        step = 0;
        movedForward = movedBack = movedLeft = movedRight = movedUp = movedDown = false;

        HintsPopupUI.I?.ShowSticky(
            $"Движение: {keyForward} {keyLeft} {keyBackward} {keyRight} — " +
            "подвигай подлодку вперёд, назад, влево и вправо."
        );
    }

    void Update()
    {
        if (!Active) return;

        switch (step)
        {
            case 0: HandleStep0_Movement(); break;
            case 1: HandleStep1_UpDown(); break;
            case 2: HandleStep2_Scan(); break;
            case 3: /* ждём OnAnyFishCaught */ break;
        }
    }

    // ---------- Шаг 0: движение ----------

    void HandleStep0_Movement()
    {
        if (Input.GetKey(keyForward)) movedForward = true;
        if (Input.GetKey(keyBackward)) movedBack = true;
        if (Input.GetKey(keyLeft)) movedLeft = true;
        if (Input.GetKey(keyRight)) movedRight = true;

        if (movedForward && movedBack && movedLeft && movedRight)
        {
            step = 1;

            HintsPopupUI.I?.ShowSticky(
                $"Подъём/спуск: {keyUp} — вверх, {keyDown} — вниз. Нажми обе."
            );
        }
    }

    // ---------- Шаг 1: вверх/вниз ----------

    void HandleStep1_UpDown()
    {
        if (Input.GetKey(keyUp)) movedUp = true;
        if (Input.GetKey(keyDown)) movedDown = true;

        if (movedUp && movedDown)
        {
            step = 2;

            HintsPopupUI.I?.ShowSticky(
                $"Скан: нажми {keyScan}."
            );
        }
    }

    // ---------- Шаг 2: скан ----------

    void HandleStep2_Scan()
    {
        if (Input.GetKeyDown(keyScan))
        {
            CompleteScanStep();
        }
    }

    void CompleteScanStep()
    {
        step = 3;

        // Дальше игра идёт в реальном времени
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
