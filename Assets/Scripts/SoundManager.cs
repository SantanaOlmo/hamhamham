using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    public AudioMixerGroup sfxOutput; // Optional: To route spawned sources

    [Header("Player SFX")]
    public AudioClip sfxShoot;
    public AudioClip sfxBomb;
    public AudioClip sfxPlayerDamage;
    public AudioClip sfxPlayerDeath;
    public AudioClip sfxDash; 

    [Header("Enemy SFX")]
    public AudioClip sfxEnemyDamage;
    public AudioClip sfxEnemyDeath; // Optional, useful
    public AudioClip sfxTurretDeath; // Custom Turret Death Sound

    [Header("Pickups SFX")]
    public AudioClip sfxHealth;
    public AudioClip sfxShield;
    public AudioClip sfxSpeed;
    public AudioClip sfxTimeStop;
    public AudioClip sfxAmmo; 
    public AudioClip sfxTurret; // New
    public AudioClip sfxTurretPlace; // Turret Deployment Sound
    public AudioClip sfxTimeStopUse;

    [Header("Music")]
    public AudioSource musicSource; // Reference to the Music AudioSource

    // AudioSource pool is overkill for this scope, we can use PlayClipAtPoint or a dedicated source.
    // Better: One AudioSource on this object for non-spatial sounds (UI, Global)
    [Header("Game SFX")]
    public AudioClip sfxRoundStart;
    public AudioClip[] sfxBossRoars; // Array of 3

    private AudioSource sfxSource;
    private AudioSource bossVoiceSource; // Dedicated channel
    private Coroutine bossRoarCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        bossVoiceSource = gameObject.AddComponent<AudioSource>(); // Separate source
        
        if (sfxOutput != null)
        {
             sfxSource.outputAudioMixerGroup = sfxOutput;
             bossVoiceSource.outputAudioMixerGroup = sfxOutput;
        }
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale);
        }
    }
    
    // Boss Roar Logic
    public void StartBossRoars()
    {
        if (bossRoarCoroutine == null)
        {
            bossRoarCoroutine = StartCoroutine(BossRoarRoutine());
        }
    }

    public void StopBossRoars()
    {
        if (bossRoarCoroutine != null)
        {
            StopCoroutine(bossRoarCoroutine);
            bossRoarCoroutine = null;
        }
    }

    System.Collections.IEnumerator BossRoarRoutine()
    {
        if (sfxBossRoars == null || sfxBossRoars.Length == 0) yield break;

        int index = 0;
        WaitForSeconds shortDelay = new WaitForSeconds(2.0f); // Gap between roars

        while (true)
        {
            AudioClip clip = sfxBossRoars[index];
            if (clip != null)
            {
                bossVoiceSource.clip = clip;
                bossVoiceSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
            
            yield return shortDelay;

            index++;
            if (index >= sfxBossRoars.Length) index = 0;
        }
    }

    public void PlayMusic()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }

    // Volume Control Methods
    // Param names must match exposed params in AudioMixer (MasterVol, MusicVol, SFXVol)
    
    public void SetMasterVolume(float level)
    {
        if (audioMixer != null) audioMixer.SetFloat("MasterVol", LogarithmicDb(level));
    }

    public void SetMusicVolume(float level)
    {
        if (audioMixer != null) audioMixer.SetFloat("MusicVol", LogarithmicDb(level));
    }

    public void SetSFXVolume(float level)
    {
        if (audioMixer != null) audioMixer.SetFloat("SFXVol", LogarithmicDb(level));
    }

    // Convert linear 0-1 slider to decibels (-80 to 0)
    private float LogarithmicDb(float linear)
    {
        if (linear <= 0.0001f) return -80f;
        return Mathf.Log10(linear) * 20f;
    }
}
