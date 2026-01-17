using UnityEngine;
using System.Collections;

public partial class GameManager : MonoBehaviour
{
    // DISCO CONFIGURATION
    [Header("Disco Mode Timing (Seconds in Song)")]
    public float discoStart1 = 52.0f;
    public float discoEnd1 = 83.0f; 
    public float discoStart2 = 143.0f; 
    public float discoEnd2 = 205.0f; 

    // Disco Mode Variables
    private bool isDiscoMode = false;
    private Coroutine discoCoroutine;
    
    // Lighting Backup
    private Color originalAmbientColor;
    private float originalAmbientIntensity;
    private float originalReflectionIntensity;
    private Color originalLightColor;
    private float originalLightIntensity;

    [Header("Disco Blackout")]
    [Range(0, 1)] public float blackScreenAlpha = 0f;

    void SyncDiscoMode()
    {
        AudioSource audio = (SoundManager.Instance != null) ? SoundManager.Instance.musicSource : null;
        if (audio != null && (audio.isPlaying || IsTimeFrozen))
        {
            float songTime = audio.time; 
            // Check Intervals
            bool inInterval1 = (songTime >= discoStart1 && songTime <= discoEnd1);
            bool inInterval2 = (songTime >= discoStart2 && songTime <= discoEnd2);
            bool shouldBeDisco = inInterval1 || inInterval2;
            
            if (shouldBeDisco != isDiscoMode)
            {
                // Debug.Log($"[GameManager] Disco Mode TOGGLE: {shouldBeDisco} at time {songTime}"); // Reduced Log spam
                SetDiscoMode(shouldBeDisco);
            }
        }
    }

    void SetDiscoMode(bool active)
    {
        isDiscoMode = active;
        
        GameObject crowdContainer = GameObject.Find("Crowd_Container");
        CrowdGenerator cg = (crowdContainer != null) ? crowdContainer.GetComponent<CrowdGenerator>() : null;

        if (active)
        {
            // Start Disco
            if(cg != null) cg.SetCrowdSpeed(13.4f); 
            
            // CAPTURE LIGHTING
            if (mainLight == null) mainLight = FindFirstObjectByType<Light>();
            if (mainLight != null)
            {
                originalLightColor = mainLight.color;
                originalLightIntensity = mainLight.intensity;
            }
            originalAmbientColor = RenderSettings.ambientLight;
            originalAmbientIntensity = RenderSettings.ambientIntensity;
            originalReflectionIntensity = RenderSettings.reflectionIntensity;

            if (discoCoroutine != null) StopCoroutine(discoCoroutine);
            discoCoroutine = StartCoroutine(DiscoLightsRoutine());
        }
        else
        {
            // End Disco
            if(cg != null) cg.SetCrowdSpeed(2.0f); 
            
            if (discoCoroutine != null) StopCoroutine(discoCoroutine);
            
            // CLEANUP VISUALS FORCEFULLY
            blackScreenAlpha = 0f; // Remove overlay
            
            // Reset Light
            if (mainLight != null)
            {
                mainLight.intensity = originalLightIntensity; 
                mainLight.color = originalLightColor;
            }
            // Restore Ambient
            RenderSettings.ambientIntensity = originalAmbientIntensity;
            RenderSettings.reflectionIntensity = originalReflectionIntensity;
            RenderSettings.ambientLight = originalAmbientColor; 
        }
    }

    IEnumerator DiscoLightsRoutine()
    {
        if (mainLight == null) mainLight = FindFirstObjectByType<Light>();
        
        float bpm = 128f;
        float beatDuration = 60f / bpm; // 0.46875s
        
        AudioSource audio = (SoundManager.Instance != null) ? SoundManager.Instance.musicSource : null;

        while (isDiscoMode)
        {
            if (IsTimeFrozen) 
            {
                yield return null; 
                continue; 
            }

            // Sync with Start Delay
            float rawTime = (audio != null) ? audio.time : GameTime;
            float syncTime = rawTime - startDelay;

            // 1. Color Cycle (Every Beat - 4x faster)
            if (mainLight != null)
            {
                 // Using beatDuration for speed
                 float currentBeat = Mathf.Floor(syncTime / beatDuration);
                 float hue = Mathf.Repeat(currentBeat * 0.25f, 1f); 
                 mainLight.color = Color.HSVToRGB(hue, 1f, 1f);
                 mainLight.intensity = 1.5f; 
            }

            yield return null; 
        }

        // Restore
        blackScreenAlpha = 0f;
        if (mainLight != null)
        {
            mainLight.intensity = originalLightIntensity;
            mainLight.color = originalLightColor;
        }
        RenderSettings.ambientLight = originalAmbientColor; 
        RenderSettings.ambientIntensity = originalAmbientIntensity;
        RenderSettings.reflectionIntensity = originalReflectionIntensity;
    }
}
