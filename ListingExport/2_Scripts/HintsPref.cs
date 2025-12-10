using UnityEngine;

public static class HintsPref
{
    const string Key = "HintsEnabled";
    public static bool Get() => PlayerPrefs.GetInt(Key, 1) == 1;     // по умолчанию ВКЛ
    public static void Set(bool v) { PlayerPrefs.SetInt(Key, v ? 1 : 0); PlayerPrefs.Save(); }
}
