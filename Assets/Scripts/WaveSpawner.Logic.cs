using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public partial class WaveSpawner : MonoBehaviour
{
    // 'gm' is defined in the main WaveSpawner.cs

    public void StartNextWave(int round)
    {
        // 1. Build the list of enemies to spawn
        List<EnemyData> enemiesToSpawn = new List<EnemyData>();
        
        // CHECK FOR SPECIAL ROUND
        // Find if this round is special
        SpecialRound? special = null;
        if (specialRounds != null)
        {
            foreach(var s in specialRounds)
            {
                if (s.roundNumber == round)
                {
                    special = s;
                    break;
                }
            }
        }

        if (special.HasValue) // IS SPECIAL ROUND
        {
            Debug.Log($"WaveSpawner: Round {round} is SPECIAL! Spawning custom config.");
            if (special.Value.waves != null)
            {
                foreach(var wave in special.Value.waves)
                {
                    if (wave.enemyData != null && wave.count > 0)
                    {
                        for(int i=0; i<wave.count; i++) enemiesToSpawn.Add(wave.enemyData);
                    }
                }
            }
        }
        else // IS NORMAL PROCEDURAL ROUND
        {
            int baseCount = enemiesPerWave + (round * additionalEnemiesPerWave);
            
            // Debug Data Validation
            if (fireTigerData == null) 
            {
                // Ensure fallback exists if referenced
                fireTigerData = ScriptableObject.CreateInstance<EnemyData>();
                fireTigerData.enemyName = "Runtime_FireTiger";
                fireTigerData.type = EnemyType.Shooter;
                fireTigerData.baseHealth = 2;
                fireTigerData.baseSpeed = 2.5f;
                fireTigerData.modelScale = 3f;
            }

            // Bosses
            int bossCount = (round >= 10) ? (round - 9) : 0;
            for(int b=0; b<bossCount; b++) enemiesToSpawn.Add(bossData);

            // Procedural Enemies
            // Simplified Logic: Add them one by one or in squads
            for(int i=0; i<baseCount; i++)
            {
                bool isFire = (round >= 5) && (fireTigerData != null) && (Random.Range(0f, 1f) < 0.2f);
                
                if (isFire)
                {
                    // Fire Tiger Squad Logic (Flattened)
                    // If it's a Fire Tiger spawn, decide if it's a squad or single
                    int count = 1;
                    if (round >= 15) count = 4;
                    else if (round >= 10) count = 2;
                    else count = 1;

                    for(int c=0; c<count; c++) enemiesToSpawn.Add(fireTigerData);
                    
                    // If we added a squad, we might want to skip upcoming loop iterations to preserve total count?
                    // User logic previously added 1 "Type 2" which then spawned 4.
                    // Meaning 1 "BaseCount" slot = 4 actual enemies.
                    // So we proceed, and this loop just fills the list larger than baseCount implies, which is fine (difficulty scaling).
                }
                else
                {
                    // Normal or Melee
                    // "Tiger" is now just a variation handled by normalData (or ID 1 if logic distinguishes)
                    if (normalData != null && Random.Range(0f, 1f) < 0.3f)
                    {
                        // Melee Tiger (Variation) - assuming handled by same data but maybe random toggle in Setup?
                        // If you strictly need different stats, you need different Data. 
                        // For now, using normalData as requested.
                        enemiesToSpawn.Add(normalData); 
                    }
                    else
                    {
                        enemiesToSpawn.Add(normalData);
                    }
                }
            }
        }
        
        int totalEnemies = enemiesToSpawn.Count;
        
        // Update GameManager Progress
        if (gm != null)
        {
            gm.TotalEnemiesThisRound = totalEnemies;
            gm.EnemiesKilledThisRound = 0;
        }
        
        // Audio: Round Start
        if (SoundManager.Instance != null && SoundManager.Instance.sfxRoundStart != null)
        {
             SoundManager.Instance.PlaySFX(SoundManager.Instance.sfxRoundStart);
        }

        Debug.Log($"Starting Round {round}. Total Enemies: {totalEnemies}");
        
        // AMMO POWERUP SPAWN (Round 7 & 14)
        if (round == 7 || round == 14)
        {
            SpawnAmmoDrop();
        }

        StartCoroutine(SpawnWaveRoutine(enemiesToSpawn));
    }

    void SpawnAmmoDrop()
    {
         Vector3 ammoPos = new Vector3(Random.Range(-30, 30), 1, Random.Range(-30, 30));
         if (gm.Player != null)
         {
              float dist = Vector3.Distance(ammoPos, gm.Player.transform.position);
              if (dist < 10f) ammoPos += Vector3.forward * 10f; 
         }
         
         if (ammoPowerUpPrefab != null)
         {
             SpawnPowerUp(ammoPos, PowerUpType.Ammo, ammoPowerUpPrefab);
         }
    }

    IEnumerator SpawnWaveRoutine(List<EnemyData> enemiesToSpawn)
    {
        foreach (EnemyData data in enemiesToSpawn)
        {
            if (gm.CurrentState != GameManager.GameState.PLAYING) yield break;
            
            // Wait if Time is Frozen
            while (gm.IsTimeFrozen) yield return null;
            
            // Specific check for Boss to wait longer?
            // Previous logic had separate boss loop. 
            // If data is bossData, wait longer?
            bool isBoss = (data == bossData); // Simple check
            
            SpawnEnemy(gm.Round, data, isBoss);

            if (isBoss) yield return new WaitForSeconds(1.5f);
            else yield return new WaitForSeconds(data.type == EnemyType.Shooter ? 0.5f : 1.0f); // Faster spawn for squads?
        }
    }
    
    void SpawnEnemy(int round, EnemyData data, bool isBoss = false)
    {
        if (data == null) 
        {
            Debug.LogWarning("SpawnEnemy received NULL data! Defaulting to NormalData.");
            data = normalData;
        }

        Debug.Log($"SpawnEnemy: Spawning {data.enemyName} (Type: {data.type}). PrefabOverride: {(data.prefabOverride != null ? data.prefabOverride.name : "null")}");

        if (data == null) return; // Fail safe

        Vector3 spawnPos = Vector3.zero;
        bool validPos = false;
        int attempts = 0;
        float minDistance = isBoss ? 40.0f : 15.0f; // Bosses spawn VERY far away 
        Vector3 playerPos = gm.Player != null ? gm.Player.transform.position : Vector3.zero;
        
        while (!validPos && attempts < 20)
        {
            spawnPos = new Vector3(Random.Range(-40, 40), 1, Random.Range(-40, 40));
            if (Vector3.Distance(spawnPos, playerPos) > minDistance) validPos = true;
            attempts++;
        }

        GameObject enemyRoot = new GameObject(data != null ? data.enemyName : "Enemy");
        enemyRoot.transform.position = spawnPos;

        // SCALE
        float finalScale = data != null ? data.modelScale : enemyScale;
        enemyRoot.transform.localScale = Vector3.one * finalScale;

        // VISUAL
        GameObject prefabToUse = (data != null && data.prefabOverride != null) ? data.prefabOverride : enemyModel;
        
        if (prefabToUse != null)
        {
            GameObject visual = Instantiate(prefabToUse, enemyRoot.transform);
            float vOffset = (data != null) ? data.visualHeightOffset : 0f;
            visual.transform.localPosition = new Vector3(0, vOffset, 0); // Apply Height Offset
            
            visual.transform.localRotation = Quaternion.Euler(enemyRotationOffset);
            
            Animator anim = visual.GetComponent<Animator>();
            if (anim == null) anim = visual.AddComponent<Animator>();
            
            if (data != null && data.animatorOverride != null) 
                anim.runtimeAnimatorController = data.animatorOverride;
            else if (enemyAnimatorController != null) 
                anim.runtimeAnimatorController = enemyAnimatorController;
        }
        
        // PHYSICS & COLLIDER
        ColliderType colType = (data != null) ? data.colliderType : ColliderType.Box;
        Vector3 colSize = (data != null) ? data.colliderSize : Vector3.one;
        Vector3 colOffset = (data != null) ? data.colliderOffset : Vector3.zero;
        bool showDebug = (data != null) && data.showCollider;

        if (colType == ColliderType.Box)
        {
            BoxCollider col = enemyRoot.AddComponent<BoxCollider>();
            col.center = colOffset;
            col.size = colSize;
            col.isTrigger = false;
        }
        else
        {
            SphereCollider col = enemyRoot.AddComponent<SphereCollider>();
            col.center = colOffset;
            col.radius = Mathf.Max(colSize.x, colSize.y, colSize.z) / 2f;
            col.isTrigger = false;
        }

        // Debug Visual for Collider
        if (showDebug)
        {
            GameObject debugCol = null;
            if (colType == ColliderType.Box)
            {
                debugCol = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debugCol.transform.SetParent(enemyRoot.transform, false);
                debugCol.transform.localPosition = colOffset;
                debugCol.transform.localScale = colSize;
            }
            else
            {
                debugCol = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugCol.transform.SetParent(enemyRoot.transform, false);
                debugCol.transform.localPosition = colOffset;
                float size = Mathf.Max(colSize.x, colSize.y, colSize.z);
                debugCol.transform.localScale = Vector3.one * size;
            }

            if (debugCol != null)
            {
                Destroy(debugCol.GetComponent<Collider>());
                Renderer r = debugCol.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material = new Material(Shader.Find("Sprites/Default"));
                    r.material.color = new Color(1, 0, 0, 0.4f); // Red for enemies
                }
            }
        } 

        Rigidbody rb = enemyRoot.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false; 
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        
        Enemy enemy = enemyRoot.AddComponent<Enemy>();
        
        // STATS
        int baseHealth = data != null ? data.baseHealth : 1;
        float baseSpeed = data != null ? data.baseSpeed : enemyBaseSpeed;
        
        int healthBonus = (data != null) ? data.healthBonusPer5Rounds : 1;
        int health = baseHealth + ((round / 5) * healthBonus);
        
        float growth = (data != null) ? data.speedGrowthPerRound : enemySpeedIncreasePerRound;
        float calculatedSpeed = baseSpeed * (1.0f + ((round - 1) * growth));
        
        float playerSpeed = (gm.Player != null && gm.Player.moveSpeed > 0.1f) ? gm.Player.moveSpeed : 8.0f;
        // Cap at 95% of player speed to ensure player can (barely) outrun them, 
        // BUT ensure we don't accidentally cap them to near-zero if player speed is weird.
        // We take the MAX of (Player*0.95) and (Base 5.0) to guarantee they at least move.
        float maxAllowedSpeed = Mathf.Max(playerSpeed * 0.95f, 5.0f); 
        
        float finalSpeed = Mathf.Min(calculatedSpeed, maxAllowedSpeed);

        if (isBoss) finalSpeed = calculatedSpeed; // Uncap boss speed if desired
        
        // DEBUG SPEED:
        Debug.Log($"[SpawnEnemy] {data?.enemyName ?? "Unknown"}: BaseSpeed={baseSpeed}, Calc={calculatedSpeed}, P_Speed={playerSpeed}, Cap={maxAllowedSpeed} -> FINAL={finalSpeed}");

        enemy.Setup(gm, gm.Player.transform, data, finalSpeed, health, isBoss, round);
        
        gm.RegisterEnemy(enemy);
    }
}
