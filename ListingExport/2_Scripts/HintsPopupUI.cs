using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HintsPopupUI : MonoBehaviour
{
    public static HintsPopupUI I;          // singleton для простых вызовов

    [Header("UI")]
    public CanvasGroup canvasGroup;        // alpha
    public TextMeshProUGUI label;          // текст подсказки

    [Header("Timings (сек)")]
    public float fadeIn = 0.15f;
    public float hold = 2.2f;
    public float fadeOut = 0.25f;

    Queue<(string msg, float hold)> q = new Queue<(string, float)>();
    Coroutine runner;
    string lastMsg = null;

    void Awake()
    {
        I = this;
        if (canvasGroup) canvasGroup.alpha = 0f;
    }

    /// Поставить подсказку в очередь (если такая же уже на экране — не дублируем).
    public void Enqueue(string msg, float customHold = -1f)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;
        if (label && lastMsg == msg && canvasGroup && canvasGroup.alpha > 0.5f) return; // анти-спам
        q.Enqueue((msg, customHold > 0 ? customHold : hold));
        if (runner == null) runner = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        while (q.Count > 0)
        {
            var (msg, h) = q.Dequeue();
            lastMsg = msg;
            if (label) label.text = msg;

            // fade in
            float t = 0;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                if (canvasGroup) canvasGroup.alpha = Mathf.SmoothStep(0, 1, t / fadeIn);
                yield return null;
            }
            if (canvasGroup) canvasGroup.alpha = 1;

            // hold
            float w = 0;
            while (w < h) { w += Time.unscaledDeltaTime; yield return null; }

            // fade out
            t = 0;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                if (canvasGroup) canvasGroup.alpha = Mathf.SmoothStep(1, 0, t / fadeOut);
                yield return null;
            }
            if (canvasGroup) canvasGroup.alpha = 0;
        }
        runner = null;
        lastMsg = null;
    }
    // В HintsPopupUI.cs
    public void ShowSticky(string msg)
    {
        StopAllCoroutines();
        if (label) label.text = msg;
        if (canvasGroup) canvasGroup.alpha = 1f;   // показываем и держим
    }

    public void HideImmediate()
    {
        StopAllCoroutines();
        if (canvasGroup) canvasGroup.alpha = 0f;   // прячем сразу
    }

}
