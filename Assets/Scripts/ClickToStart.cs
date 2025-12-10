using UnityEngine;

public class ClickToStart : MonoBehaviour
{
    public GameObject overlayPanel; // панель "Нажмите, чтобы начать"

    public void OnClickStart()
    {
        if (overlayPanel) overlayPanel.SetActive(false);

#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = true;
#endif

        // если у тебя шутерное управление:
#if UNITY_STANDALONE || UNITY_WEBGL
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
#endif
    }
}
