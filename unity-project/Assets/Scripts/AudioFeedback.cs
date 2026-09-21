using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioFeedback : MonoBehaviour
{
    private AudioSource source;
    private AudioClip collectClip;
    private AudioClip hazardClip;
    private AudioClip successClip;

    private void Awake()
    {
        EnsureReady();
    }

    private void EnsureReady()
    {
        if (source == null)
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
        }

        collectClip ??= CreateTone("Collect", 660f, 0.10f, 0.20f);
        hazardClip ??= CreateTone("Hazard", 180f, 0.18f, 0.24f);
        successClip ??= CreateTone("Success", 880f, 0.30f, 0.18f);
    }

    public void PlayCollect()
    {
        EnsureReady();
        source.PlayOneShot(collectClip);
    }

    public void PlayHazard()
    {
        EnsureReady();
        source.PlayOneShot(hazardClip);
    }

    public void PlaySuccess()
    {
        EnsureReady();
        source.PlayOneShot(successClip);
    }

    private static AudioClip CreateTone(string clipName, float frequency,
        float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;
            float fade = 1f - (float)i / sampleCount;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time)
                * volume * fade;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1,
            sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
