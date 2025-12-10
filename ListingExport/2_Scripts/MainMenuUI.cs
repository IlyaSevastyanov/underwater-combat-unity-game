using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scenes")]
    // Имя сцены с геймплеем (куда грузим по Start)
    public string gameSceneName = "Game";

    [Header("UI Panels")]
    // Панель с кнопками "НАЧАТЬ / ВЫЙТИ / НАСТРОЙКИ"
    public GameObject mainPanel;
    // Панель "НАСТРОЙКИ"
    public GameObject settingsPanel;

    [Header("Audio")]
    // Ссылка на объект с музыкой меню (MenuAudio с компонентом MenuMusic)
    public MenuMusic menuMusic;

    void Start()
    {
        // показать главное меню и спрятать настройки
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // Кнопка "НАЧАТЬ ИГРУ"
    public void StartGame()
    {
        // перед переходом в игру — остановить и уничтожить музыку меню
        if (menuMusic != null)
        {
            menuMusic.StopAndDestroy();
        }

        SceneManager.LoadScene(gameSceneName);
    }

    // Кнопка "НАСТРОЙКИ"
    public void OpenSettings()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    // Кнопка "НАЗАД" в настройках
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    // Кнопка "ВЫЙТИ"
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit requested");
    }
}
