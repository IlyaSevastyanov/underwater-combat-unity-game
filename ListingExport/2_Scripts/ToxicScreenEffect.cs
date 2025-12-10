using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ToxicScreenEffect : MonoBehaviour
{
    [Header("Overlay")]
    public Image overlayImage;     // ссылка на Image

    [Header("Blink settings")]
    public int blinkCount = 3;
    public float fadeInTime = 0.1f;
    public float holdTime = 0.05f;
    public float fadeOutTime = 0.1f;
    public float betweenBlinks = 0.05f;
    [Range(0f, 1f)]
    public float maxAlpha = 0.7f;

    void Awake()
    {
        if (overlayImage == null)
            overlayImage = GetComponent<Image>();

        if (overlayImage != null)
        {
            var c = overlayImage.color;
            c.a = 0f;                // изначально полностью прозрачный
            overlayImage.color = c;
        }
    }

    public void Play()
    {
        if (overlayImage == null) return;

        StopAllCoroutines();
        StartCoroutine(DoEffect());
    }

    IEnumerator DoEffect()
    {
        Color c = overlayImage.color;

        for (int i = 0; i < blinkCount; i++)
        {
            // затемнение
            float t = 0f;
            while (t < fadeInTime)
            {
                t += Time.deltaTime;
                float k = fadeInTime > 0f ? t / fadeInTime : 1f;
                c.a = Mathf.Lerp(0f, maxAlpha, k);
                overlayImage.color = c;
                yield return null;
            }

            if (holdTime > 0f)
                yield return new WaitForSeconds(holdTime);

            // высветление
            t = 0f;
            while (t < fadeOutTime)
            {
                t += Time.deltaTime;
                float k = fadeOutTime > 0f ? t / fadeOutTime : 1f;
                c.a = Mathf.Lerp(maxAlpha, 0f, k);
                overlayImage.color = c;
                yield return null;
            }

            c.a = 0f;
            overlayImage.color = c;

            if (betweenBlinks > 0f && i < blinkCount - 1)
                yield return new WaitForSeconds(betweenBlinks);
        }
    }
}
