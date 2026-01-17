using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public partial class GameManager : MonoBehaviour
{
    // NO Variables defined here except logic-local ones if needed.
    // Core variables (Score, Enemies, etc.) are in Main file.

    public void RegisterEnemy(Enemy e)
    {
        enemies.Add(e);
        if (e.IsBoss)
        {
             if (SoundManager.Instance != null) SoundManager.Instance.StartBossRoars();
        }
    }

    public void OnEnemyKilled(Enemy enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
            Score++;
            EnemiesKilledThisRound++;

            if (!IsAnyBossActive())
            {
                 if (SoundManager.Instance != null) SoundManager.Instance.StopBossRoars();
            }

            // Auto-Grant Bomb every 100 kills
            if (Score > 0 && Score % 100 == 0)
            {
                EnableSpecialAbility();
            }

            if (waveSpawner != null) waveSpawner.CheckDrop(enemy.transform.position, Score);
        }
    }

    public void EnableSpecialAbility()
    {
        bombStoredCount++;
        Debug.Log("BOMB ABILITY ENABLED!");
    }

    IEnumerator ActivateSpecialAbility()
    {
        if (SoundManager.Instance != null && SoundManager.Instance.sfxBomb != null)
        {
             SoundManager.Instance.PlaySFX(SoundManager.Instance.sfxBomb);
        }

        bombStoredCount--;

        if (SoundManager.Instance != null) SoundManager.Instance.PauseMusic();
        IsTimeFrozen = true; 
        Time.timeScale = 0f;

        // Visual
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = Player.transform.position;
        Destroy(sphere.GetComponent<Collider>());
        Renderer r = sphere.GetComponent<Renderer>();
        r.material = new Material(Shader.Find("Sprites/Default"));
        r.material.color = new Color(1f, 1f, 0f, 0.4f);

        float duration = 2.0f;
        float timer = 0f;
        float maxRadius = 20f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / duration;
            float currentScale = Mathf.Lerp(1f, maxRadius * 2, progress);
            sphere.transform.localScale = Vector3.one * currentScale;

            float currentRadius = currentScale / 2f;
            foreach (var enemy in new List<Enemy>(enemies))
            {
                if (enemy != null && Vector3.Distance(Player.transform.position, enemy.transform.position) <= currentRadius)
                {
                    enemy.Die();
                }
            }
            yield return null;
        }

        Destroy(sphere);
        
        if (SoundManager.Instance != null) SoundManager.Instance.ResumeMusic();
        IsTimeFrozen = false;
        Time.timeScale = 1f;
    }

    public void FreezeTime(float duration)
    {
        StartCoroutine(FreezeRoutine(duration));
    }
    
    IEnumerator FreezeRoutine(float duration)
    {
        IsTimeFrozen = true;
        
        if (SoundManager.Instance != null) 
        {
            SoundManager.Instance.PauseMusic();
            if (SoundManager.Instance.sfxTimeStopUse != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.sfxTimeStopUse);
        }

        foreach(var e in enemies) if(e != null) e.Freeze(true);

        yield return new WaitForSeconds(duration);

        if (SoundManager.Instance != null) SoundManager.Instance.ResumeMusic();

        foreach(var e in enemies) if(e != null) e.Freeze(false);
        
        IsTimeFrozen = false;
    }

    public void StoreTimeStop()
    {
        timeStopStoredCount++;
    }

    public void ActivateTimeStop()
    {
        if (HasTimeStopStored && !IsTimeFrozen)
        {
            timeStopStoredCount--;
            FreezeTime(10f); 
        }
    }

    void ClearEnemies()
    {
        foreach (var e in enemies) if (e != null) Destroy(e.gameObject);
        enemies.Clear();
        if (SoundManager.Instance != null) SoundManager.Instance.StopBossRoars();
    }
    
    bool IsAnyBossActive()
    {
        foreach(var e in enemies)
        {
            if (e != null && e.IsBoss) return true;
        }
        return false;
    }

    IEnumerator PrepareNextWave()
    {
        waitingForWave = true;
        Debug.Log("Wave Cleared! Waiting 3 seconds...");
        yield return new WaitForSeconds(3.0f);
        StartNextWave();
        waitingForWave = false;
    }

    void StartNextWave()
    {
        Round++;
        if (waveSpawner != null) waveSpawner.StartNextWave(Round);
    }
}
