using UnityEngine;

public class MobilePerfBootstrap : MonoBehaviour
{
    void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (WebGLBrowserCheck.IsMobileBrowser())
        {
            Application.targetFrameRate = 30;
        }
#elif UNITY_ANDROID || UNITY_IOS
        Application.targetFrameRate = 30;
#endif
    }
}
