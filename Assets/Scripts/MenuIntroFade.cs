using UnityEngine;
using System.Collections;

public class MenuIntroFade : MonoBehaviour
{
    public CanvasGroup group;
    public float fadeTime = 0.5f;
    public float startDelay = 0.2f;
    public Vector3 startScale = new Vector3(1.05f, 1.05f, 1f);

    void Reset()
    {
        group = GetComponent<CanvasGroup>();
    }

    void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        transform.localScale = startScale;
    }

    void OnEnable()
    {
        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        yield return new WaitForSeconds(startDelay);

        float t = 0f;
        Vector3 scaleFrom = transform.localScale;
        Vector3 scaleTo = Vector3.one;
        float alphaFrom = 0f;
        float alphaTo = 1f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float k = t / fadeTime;
            // плавно, но не слишком по-детскому — ease out
            float ease = 1f - Mathf.Pow(1f - k, 3f);

            group.alpha = Mathf.Lerp(alphaFrom, alphaTo, ease);
            transform.localScale = Vector3.Lerp(scaleFrom, scaleTo, ease);

            yield return null;
        }

        group.alpha = 1f;
        transform.localScale = Vector3.one;
    }
}
