using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("Desktop UI")]
    public GameObject desktopPauseMenuPanel;     // System/PauseSystem/DesktopRoot/PauseMenuPanel
    public GameObject desktopSettingsPanel;      // System/PauseSystem/DesktopRoot/SettingsPanel
    public Button desktopResumeButton;           // ResumeButton
    public Button desktopMainMenuButton;         // MainMenuButton
    public Button desktopQuitButton;             // QuitButton
    public Button desktopSettingsButton;         // SettingsButton (внутри PauseMenuPanel)

    [Header("Mobile UI")]
    public GameObject mobilePauseMenuPanel;      // System/PauseSystem/MobileRoot/PauseMenuPanel
    public GameObject mobileSettingsPanel;       // System/PauseSystem/MobileRoot/SettingsPanel
    public Button mobileResumeButton;
    public Button mobileMainMenuButton;
    public Button mobileQuitButton;              // можно не заполнять
    public Button mobileSettingsButton;

    [Header("Common")]
    [Tooltip("Имя сцены главного меню.")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("В редакторе использовать мобильный UI (для отладки).")]
    public bool forceMobileUiInEditor = false;

    [Header("Player")]
    [Tooltip("Ссылка на контроллер субмарины (Player). Если не указать — найдётся по тегу Player.")]
    public SubmarineController submarineController;

    // --- внутренние ссылки на активный сет UI ---
    GameObject pauseMenuPanel;
    GameObject settingsPanel;
    Button resumeButton;
    Button mainMenuButton;
    Button quitButton;
    Button settingsButton;

    bool isPaused;
    bool isMobileLike;

    void Awake()
    {
        // 1) определяем, какой UI должен использоваться
        bool isMobileRuntime = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: спрашиваем JS-плагин, мобильный ли браузер
        isMobileRuntime = WebGLBrowserCheck.IsMobileBrowser();
#elif UNITY_ANDROID || UNITY_IOS
        // Нативные мобильные платформы
        isMobileRuntime = true;
#else
        // Остальное — десктоп
        isMobileRuntime = false;
#endif

        isMobileLike = isMobileRuntime || (Application.isEditor && forceMobileUiInEditor);

        Debug.Log($"[PauseMenu] platform={Application.platform}, " +
                  $"isMobileRuntime={isMobileRuntime}, " +
                  $"forceMobileUiInEditor={forceMobileUiInEditor}, " +
                  $"isMobileLike={isMobileLike}");

        // 2) гасим ВСЕ панели на старте (и десктоп, и мобайл)
        if (desktopPauseMenuPanel) desktopPauseMenuPanel.SetActive(false);
        if (desktopSettingsPanel) desktopSettingsPanel.SetActive(false);
        if (mobilePauseMenuPanel) mobilePauseMenuPanel.SetActive(false);
        if (mobileSettingsPanel) mobileSettingsPanel.SetActive(false);

        // 3) выбираем активный набор UI
        if (isMobileLike && mobilePauseMenuPanel != null)
        {
            pauseMenuPanel = mobilePauseMenuPanel;
            settingsPanel = mobileSettingsPanel;
            resumeButton = mobileResumeButton;
            mainMenuButton = mobileMainMenuButton;
            quitButton = mobileQuitButton;
            settingsButton = mobileSettingsButton;
        }
        else
        {
            pauseMenuPanel = desktopPauseMenuPanel;
            settingsPanel = desktopSettingsPanel;
            resumeButton = desktopResumeButton;
            mainMenuButton = desktopMainMenuButton;
            quitButton = desktopQuitButton;
            settingsButton = desktopSettingsButton;
        }

        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);

        WireButtons();
        CacheSubmarine();
    }

    void WireButtons()
    {
        if (resumeButton)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResume);
        }

        if (mainMenuButton)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenu);
        }

        if (quitButton)
        {
            quitButton.onClick.RemoveAllListeners();

            // Кнопка выхода имеет смысл только в Standalone билдах (ПК)
#if UNITY_STANDALONE && !UNITY_EDITOR
            quitButton.gameObject.SetActive(true);
            quitButton.onClick.AddListener(OnQuit);
#else
            // На Android / iOS / WebGL / в редакторе – прячем
            quitButton.gameObject.SetActive(false);
#endif
        }

        if (settingsButton)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettings);
        }
    }


    void CacheSubmarine()
    {
        if (!submarineController)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) submarineController = player.GetComponent<SubmarineController>();
        }
    }

    void Update()
    {
        // ESC на ПК = Back на Android (на мобилках WebGL это почти не актуально, но не мешает)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[PauseMenu] Escape pressed");

            if (!isPaused)
            {
                OnPause();
            }
            else
            {
                // если открыта панель настроек – закрываем её, остаёмся в паузе
                if (settingsPanel && settingsPanel.activeSelf)
                    CloseSettings();
                else
                    OnResume();
            }
        }
    }

    /// <summary>
    /// Вызывай из кнопки Pause (и на ПК, и на мобилке).
    /// </summary>
    public void TogglePause()
    {
        Debug.Log("[PauseMenu] TogglePause()");
        if (isPaused) OnResume();
        else OnPause();
    }

    public void OnPause()
    {
        Debug.Log("[PauseMenu] OnPause()");
        if (isPaused) return;
        isPaused = true;

        // при входе в паузу настройки всегда скрыты
        if (settingsPanel) settingsPanel.SetActive(false);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(true);

        Time.timeScale = 0f;
        ToggleSubmarineControl(false);

#if UNITY_STANDALONE || UNITY_EDITOR
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#endif
    }


    public void OnResume()
    {
        Debug.Log("[PauseMenu] OnResume()");
        if (!isPaused) return;
        isPaused = false;

        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);

        Time.timeScale = 1f;
        ToggleSubmarineControl(true);

#if UNITY_STANDALONE || UNITY_EDITOR
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
#endif
    }

    // --- Настройки ---

    public void OpenSettings()
    {
        Debug.Log("[PauseMenu] OpenSettings()");
        if (!isPaused) return; // настройки доступны только во время паузы

        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (settingsPanel)
        {
            settingsPanel.SetActive(true);
            settingsPanel.transform.SetAsLastSibling(); // наверх в Canvas
        }
    }
    public void CloseSettings()
    {
        Debug.Log("[PauseMenu] CloseSettings()");
        if (settingsPanel) settingsPanel.SetActive(false);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
    }

    // --- Переходы / выход ---

    public void OnMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnQuit()
    {
        Application.Quit();
        Debug.Log("[PauseMenu] Quit requested");
    }

    void ToggleSubmarineControl(bool enabled)
    {
        CacheSubmarine();
        if (submarineController)
            submarineController.enabled = enabled;
    }
}
