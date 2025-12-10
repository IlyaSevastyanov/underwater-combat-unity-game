using UnityEngine;

public class PlatformRootSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject desktopRoot;
    [SerializeField] private GameObject mobileRoot;

    void Awake()
    {
        bool isMobileRuntime = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: смотрим, мобильный ли браузер
        isMobileRuntime = WebGLBrowserCheck.IsMobileBrowser();
#elif UNITY_ANDROID || UNITY_IOS
        // Нативные мобилки
        isMobileRuntime = true;
#else
        // Остальное — десктоп
        isMobileRuntime = false;
#endif

        if (desktopRoot) desktopRoot.SetActive(!isMobileRuntime);
        if (mobileRoot) mobileRoot.SetActive(isMobileRuntime);

        Debug.Log($"[PlatformRootSwitcher] platform={Application.platform}, " +
                  $"isMobileRuntime={isMobileRuntime}, " +
                  $"desktopActive={desktopRoot?.activeInHierarchy}, " +
                  $"mobileActive={mobileRoot?.activeInHierarchy}");
    }
}
