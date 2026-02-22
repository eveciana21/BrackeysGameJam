using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;

    [Header("SFX Settings")]
    [SerializeField] private int sfxPoolSize = 5;
    [SerializeField] private float sfxVolume = 1f;

    [Header("Music Settings")]
    [SerializeField] private float crossfadeDuration = 1.5f;
    [SerializeField] private float musicVolume = 1f;

    private AudioSource[] sfxPool;
    private int sfxPoolIndex = 0;

    private AudioSource activeMusic;
    private AudioSource inactiveMusic;
    private Coroutine crossfadeRoutine;

    private void Awake()
    {
        if (coreManagersChannel != null)
            coreManagersChannel.SetAudioManager(this);

        // Build SFX pool
        sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            sfxPool[i] = gameObject.AddComponent<AudioSource>();
            sfxPool[i].playOnAwake = false;
        }

        // Start with A as active
        activeMusic = musicSourceA;
        inactiveMusic = musicSourceB;
        musicSourceA.volume = 0f;
        musicSourceB.volume = 0f;
    }

    // MUSIC 

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (activeMusic.clip == clip && activeMusic.isPlaying) return;

        if (crossfadeRoutine != null)
            StopCoroutine(crossfadeRoutine);

        crossfadeRoutine = StartCoroutine(Crossfade(clip));
    }

    private IEnumerator Crossfade(AudioClip newClip)
    {
        float elapsed = 0f;
        float startVolume = activeMusic.volume;
        bool fadeInStarted = false;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / crossfadeDuration);
            activeMusic.volume = Mathf.Lerp(startVolume, 0f, t);

            if (!fadeInStarted && activeMusic.volume <= startVolume * 0.35f)
            {
                fadeInStarted = true;
                inactiveMusic.clip = newClip;
                inactiveMusic.loop = true;
                inactiveMusic.volume = 0f;
                inactiveMusic.Play();
                StartCoroutine(FadeIn(inactiveMusic));
            }

            yield return null;
        }

        activeMusic.Stop();
        activeMusic.clip = null;
        activeMusic.volume = 0f;

        // Swap references
        AudioSource temp = activeMusic;
        activeMusic = inactiveMusic;
        inactiveMusic = temp;

        crossfadeRoutine = null;
    }

    private IEnumerator FadeIn(AudioSource source)
    {
        float elapsed = 0f;
        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, musicVolume, elapsed / crossfadeDuration);
            yield return null;
        }
        source.volume = musicVolume;
    }

    public void StopMusic()
    {
        if (crossfadeRoutine != null)
            StopCoroutine(crossfadeRoutine);

        activeMusic.Stop();
        inactiveMusic.Stop();
    }

    // SFX

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchVariance = 0.1f)
    {
        if (clip == null) return;
        AudioSource source = sfxPool[sfxPoolIndex % sfxPoolSize];
        sfxPoolIndex++;
        source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        source.volume = sfxVolume * volume;
        source.PlayOneShot(clip);
    }
}
