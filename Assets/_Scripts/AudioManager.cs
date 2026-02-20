using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private CoreManagersChannelSO coreManagersChannel;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSourceA;   // rename your existing one
    [SerializeField] private AudioSource musicSourceB;   // new second source
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Settings")]
    [SerializeField] private float crossfadeDuration = 1.5f;
    [SerializeField] private float musicVolume = 1f;

    [Header("SFX Library")]
    [SerializeField] private AudioClip[] sfxClips;

    private AudioSource activeMusic;
    private AudioSource inactiveMusic;
    private Coroutine crossfadeRoutine;

    private void Awake()
    {
        if (coreManagersChannel != null)
            coreManagersChannel.SetAudioManager(this);

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

        // Fade out active track
        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / crossfadeDuration);
            activeMusic.volume = Mathf.Lerp(startVolume, 0f, t);

            // Start fading in new track once old track hits 50%
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

    /*private IEnumerator Crossfade(AudioClip newClip)
    {
        // Fade out active
        float elapsed = 0f;
        float startVolume = activeMusic.volume;
        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            activeMusic.volume = Mathf.Lerp(startVolume, 0f, elapsed / crossfadeDuration);
            yield return null;
        }
        activeMusic.Stop();
        activeMusic.clip = null;

        // Fade in new track
        inactiveMusic.clip = newClip;
        inactiveMusic.loop = true;
        inactiveMusic.volume = 0f;
        inactiveMusic.Play();

        elapsed = 0f;
        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            inactiveMusic.volume = Mathf.Lerp(0f, musicVolume, elapsed / crossfadeDuration);
            yield return null;
        }
        inactiveMusic.volume = musicVolume;

        activeMusic = inactiveMusic;
        inactiveMusic = activeMusic;
    }*/

    public void StopMusic()
    {
        if (crossfadeRoutine != null)
            StopCoroutine(crossfadeRoutine);

        activeMusic.Stop();
        inactiveMusic.Stop();
    }

    // SFX
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
