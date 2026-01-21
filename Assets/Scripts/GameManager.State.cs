using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public partial class GameManager : MonoBehaviour
{
    // Intro Variables defined here are fine as long as NOT in Main
    [HideInInspector] public int countdownValue = 0;
    [HideInInspector] public bool showGoImage = false;
    [HideInInspector] public float goImageAlpha = 1f;
    public bool IsIntroSequence { get; private set; } = false;
    
    // Game Over Fade
    [HideInInspector] public float gameOverAlpha = 0f;

    public void TogglePause()
    {
        if (CurrentState == GameState.PLAYING)
        {
            CurrentState = GameState.PAUSED;
            Time.timeScale = 0f;
            ShowControlsInPause = false;
        }
        else if (CurrentState == GameState.PAUSED)
        {
            CurrentState = GameState.PLAYING;
            Time.timeScale = 1f;
        }
    }

    public void StartGame()
    {
        StopAllCoroutines(); 
        if (waveSpawner != null) waveSpawner.StopAllCoroutines();

        CurrentState = GameState.PLAYING;
        Score = 0;
        Round = 0; // Fix: Start at 0 so first wave is Round 1
        GameTime = 0;
        bombStoredCount = 0;
        
        // Reset PowerUps
        timeStopStoredCount = 0;
        IsTimeFrozen = false; 
        turretStoredCount = 0; 
        
        // Reset Round Progress
        TotalEnemiesThisRound = 0;
        EnemiesKilledThisRound = 0;

        gameOverAlpha = 0f; 
        Time.timeScale = 1f;
        scoreSubmitted = false;
        
        ClearEnemies(); // In Combat partial
        enemies.Clear();
        CleanupScene(); // In Resources partial
        
        if (Player == null) BootstrapScene(); // In Resources partial
        else {
             Player.transform.position = Vector3.zero;
             Player.HealFull();
             Player.gameObject.SetActive(true);
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayMusic();

        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.black;
        }

        waitingForWave = true;
        StartCoroutine(StartFirstWaveRoutine());
    }

    public void GameOver()
    {
        LastScore = Score;
        SubmitScore(LastScore); // In Resources partial
        
        CurrentState = GameState.GAMEOVER;
        
        StopMusic(); // In Resources partial
        if (SoundManager.Instance != null) SoundManager.Instance.StopBossRoars();
        
        Time.timeScale = 0f;
        
        StartCoroutine(GameOverSequence());
    }
    
    public void GoToMenu()
    {
        CurrentState = GameState.MENU;
        StopAllCoroutines();
        if (waveSpawner != null) waveSpawner.StopAllCoroutines();
        StopMusic();
        if (SoundManager.Instance != null) SoundManager.Instance.StopBossRoars();
        ClearEnemies();
        if (Player != null) Destroy(Player.gameObject);
        
        Time.timeScale = 1f; // Ensure time is running for menu animations etc
        
        SubmitScore(Score); // Save Score when manually exiting
    }

    void OnApplicationQuit()
    {
        SubmitScore(Score);
    }

    public void StopGame() 
    {
       StopAllCoroutines();
       if (waveSpawner != null) waveSpawner.StopAllCoroutines();
    }

    IEnumerator StartFirstWaveRoutine()
    {
        if (Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null)
            {
                IsIntroSequence = true; 
                cam.StartIntro(this.startDelay);
            }
        }
        
        float countdownDuration = 3.0f;
        float videoDuration = Mathf.Max(0f, this.startDelay - countdownDuration);
        
        yield return new WaitForSeconds(videoDuration);

        // Countdown
        countdownValue = 3;
        while (countdownValue > 0)
        {
            yield return new WaitForSeconds(1.0f);
            countdownValue--;
        }
        
        showGoImage = true;
        goImageAlpha = 1f;
        IsIntroSequence = false; 

        if (CurrentState == GameState.PLAYING)
        {
            waitingForWave = false; 
            StartNextWave(); // In Combat/Wave
        }
        
        StartCoroutine(AnimateGoImage());
    }

    IEnumerator AnimateGoImage()
    {
        float duration = 1.0f; 
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            goImageAlpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        
        showGoImage = false;
    }

    private IEnumerator GameOverSequence()
    {
        gameOverAlpha = 0f;
        float duration = 2.0f;
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; 
            gameOverAlpha = Mathf.Clamp01(timer / duration);
            yield return null;
        }
        gameOverAlpha = 1f;
    }
}
