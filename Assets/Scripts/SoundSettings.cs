using UnityEngine;

public static class SoundSettings
{
    const string KEY_MASTER = "Sound_Master";
    const string KEY_MUSIC = "Sound_Music";
    const string KEY_ENGINE = "Sound_Engine";
    const string KEY_SFX = "Sound_SFX";
    const string KEY_SONAR = "Sound_Sonar";

    static float GetClamped(string key, float def)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(key, def));
    }

    static void SetClamped(string key, float value)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }

    public static float GetMaster() => GetClamped(KEY_MASTER, 1f);
    public static float GetMusic() => GetClamped(KEY_MUSIC, 1f);
    public static float GetEngine() => GetClamped(KEY_ENGINE, 1f);
    public static float GetSfx() => GetClamped(KEY_SFX, 1f);
    public static float GetSonar() => GetClamped(KEY_SONAR, 1f);

    public static void SetMaster(float v) => SetClamped(KEY_MASTER, v);
    public static void SetMusic(float v) => SetClamped(KEY_MUSIC, v);
    public static void SetEngine(float v) => SetClamped(KEY_ENGINE, v);
    public static void SetSfx(float v) => SetClamped(KEY_SFX, v);
    public static void SetSonar(float v) => SetClamped(KEY_SONAR, v);
}
