using System.Collections.Generic;
using UnityEngine;

public static class HintsRuntime
{
    // антиспам по ключам
    static readonly Dictionary<string, float> lastShown = new Dictionary<string, float>();

    /// Показать подсказку (учитывает мастер-тоггл HintsPref)
    public static void Show(string msg, float hold = -1f, string cooldownKey = null, float cooldownSec = 0f)
    {
        if (!HintsPref.Get()) return;

        if (!string.IsNullOrEmpty(cooldownKey) && cooldownSec > 0f)
        {
            float now = Time.unscaledTime;
            if (lastShown.TryGetValue(cooldownKey, out var last) && now - last < cooldownSec) return;
            lastShown[cooldownKey] = now;
        }

        if (HintsPopupUI.I) HintsPopupUI.I.Enqueue(msg, hold);
    }

    /// Показать только один раз (запоминается в PlayerPrefs по ключу)
    public static void ShowOnce(string key, string msg, float hold = -1f)
    {
        if (!HintsPref.Get()) return;
        string pkey = "hint_once_" + key;
        if (PlayerPrefs.GetInt(pkey, 0) == 1) return;
        PlayerPrefs.SetInt(pkey, 1);
        PlayerPrefs.Save();
        if (HintsPopupUI.I) HintsPopupUI.I.Enqueue(msg, hold);
    }
}
