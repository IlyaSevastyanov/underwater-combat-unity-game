using UnityEngine;

public static class SoundPref
{
    private const string KEY = "SoundEnabled";

    // По умолчанию звук ВКЛ (1)
    public static bool Get()
    {
        return PlayerPrefs.GetInt(KEY, 1) == 1;
    }

    public static void Set(bool value)
    {
        PlayerPrefs.SetInt(KEY, value ? 1 : 0);
        PlayerPrefs.Save();
    }
}
