using UnityEngine;

public static class SaveService
{
    // Свой префикс не затрагивает сохранения других учебных проектов.
    private const string Prefix = "PP12.Crystal.v1.";
    public static int Unlocked => Mathf.Clamp(
        PlayerPrefs.GetInt(Prefix + "Unlocked", 1), 1, 3);
    public static float Volume => Mathf.Clamp01(
        PlayerPrefs.GetFloat(Prefix + "Volume", 0.7f));

    public static int Best(int level) => Mathf.Max(0,
        PlayerPrefs.GetInt(Prefix + "Best" + level, 0));

    public static void Complete(int level, int score)
    {
        PlayerPrefs.SetInt(Prefix + "Unlocked",
            Mathf.Max(Unlocked, Mathf.Min(level + 1, 3)));
        if (score > Best(level))
            PlayerPrefs.SetInt(Prefix + "Best" + level, score);
        PlayerPrefs.Save();
    }

    public static void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(Prefix + "Volume", value);
        PlayerPrefs.Save();
    }
}
