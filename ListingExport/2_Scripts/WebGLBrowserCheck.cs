using UnityEngine;
using System.Runtime.InteropServices;

public static class WebGLBrowserCheck
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int IsMobilePlatform();
#endif

    /// <summary>
    /// true Ц если WebGL запущен в мобильном браузере,
    /// false Ц во всех остальных случа€х (ѕ , редактор, не WebGL).
    /// </summary>
    public static bool IsMobileBrowser()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return IsMobilePlatform() != 0;
#else
        return false;
#endif
    }
}
