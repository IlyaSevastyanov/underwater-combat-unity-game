using UnityEngine;

public static class PlatformDetector
{
    public static bool IsMobileLike()
    {
#if UNITY_ANDROID || UNITY_IOS
        return true;
#elif UNITY_WEBGL && !UNITY_EDITOR
        // настоящая проверка: мобильный ли браузер
        return WebGLBrowserCheck.IsMobileBrowser();
#else
        return false;
#endif
    }

    // Вспомогательный метод для логов (можно удалить позже)
    public static bool IsMobileLikeWithLog(string who)
    {
        bool result = IsMobileLike();
        Debug.Log($"[PlatformDetector] {who}: " +
                  $"platform={Application.platform}, " +
                  $"screen={Screen.width}x{Screen.height}, " +
                  $"isMobileLike={result}");
        return result;
    }
}
