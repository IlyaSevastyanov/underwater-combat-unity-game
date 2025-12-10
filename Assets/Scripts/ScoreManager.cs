using UnityEngine;   // ← вот это нужно для MonoBehaviour, Header и т.п.
using TMPro;        // ← а это для TextMeshProUGUI

public class ScoreManager : MonoBehaviour
{
    [Header("Score")]
    public int score = 0;

    [Header("UI")]
    [Tooltip("Текст очков на ПК UI (Score12).")]
    public TextMeshProUGUI desktopScoreText;

    [Tooltip("Текст очков на мобильном UI (Score11).")]
    public TextMeshProUGUI mobileScoreText;

    void Start()
    {
        RefreshUI();
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0) score = 0;
        RefreshUI();
    }

    void RefreshUI()
    {
        string text = "ОЧКИ: " + score;

        if (desktopScoreText != null)
            desktopScoreText.text = text;

        if (mobileScoreText != null)
            mobileScoreText.text = text;
    }
}
