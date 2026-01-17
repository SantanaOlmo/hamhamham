using UnityEngine;
using System.Collections.Generic;

public partial class WaveSpawner : MonoBehaviour
{
    // Drop Queue
    private Queue<PowerUpType> deferredDrops = new Queue<PowerUpType>();

    public void CheckDrop(Vector3 position, int score)
    {
        // 1. Identify which drops triggered NOW
        List<PowerUpType> candidates = new List<PowerUpType>();

        if (score % healthSpawnRate == 0) candidates.Add(PowerUpType.Health);
        
        if (score % speedSpawnRate == 0)
        {
             if (gm != null && gm.Player != null && gm.Player.SpeedUpgradeCount < gm.maxSpeedUpgrades)
             {
                 candidates.Add(PowerUpType.Speed);
             }
        }
        
        if (score % shieldSpawnRate == 0)
        {
             if (gm != null && gm.Player != null && gm.Player.ShieldCharges < gm.maxShieldCharges)
             {
                 candidates.Add(PowerUpType.Shield);
             }
        }
        
        if (score % timeStopSpawnRate == 0)
        {
            if (gm != null && gm.timeStopStoredCount < gm.maxTimeStopStacks)
            {
                 candidates.Add(PowerUpType.TimeStop);
            }
        }
        
        if (score % turretSpawnRate == 0) // New
        {
            if (gm != null && gm.turretStoredCount < gm.maxTurretStacks)
            {
                 candidates.Add(PowerUpType.Turret);
            }
        }
        
        // 2. Add to Queue
        foreach(var c in candidates) deferredDrops.Enqueue(c);

        // 3. Process Queue (One per kill)
        if (deferredDrops.Count > 0)
        {
            PowerUpType nextDrop = deferredDrops.Dequeue();
            
             // Double check limits just in case (e.g. if picked up while in queue)
             if (nextDrop == PowerUpType.TimeStop && gm != null && gm.timeStopStoredCount >= gm.maxTimeStopStacks) return;
             if (nextDrop == PowerUpType.Turret && gm != null && gm.turretStoredCount >= gm.maxTurretStacks) return;
             if (nextDrop == PowerUpType.Shield && gm != null && gm.Player != null && gm.Player.ShieldCharges >= gm.maxShieldCharges) return;
             if (nextDrop == PowerUpType.Speed && gm != null && gm.Player != null && gm.Player.SpeedUpgradeCount >= gm.maxSpeedUpgrades) return;
             
             // MAP TYPE TO PREFAB
             GameObject prefab = null;
             switch(nextDrop)
             {
                 case PowerUpType.Health: prefab = healthPowerUpPrefab; break;
                 case PowerUpType.Speed: prefab = speedPowerUpPrefab; break;
                 case PowerUpType.Shield: prefab = shieldPowerUpPrefab; break;
                 case PowerUpType.TimeStop: prefab = timeStopPowerUpPrefab; break;
                 case PowerUpType.Ammo: prefab = ammoPowerUpPrefab; break;
                 case PowerUpType.Turret: prefab = turretPowerUpPrefab; break; // New
             }
             
             // Always spawn. If prefab is null, PowerUp.cs creates a fallback cube.
             SpawnPowerUp(position, nextDrop, prefab);
        }
    }

    void SpawnPowerUp(Vector3 position, PowerUpType type, GameObject prefab)
    {
        GameObject puGo = new GameObject("PowerUp_" + type.ToString());
        puGo.transform.position = new Vector3(position.x, Mathf.Max(position.y, powerUpBaseHeight), position.z);
        puGo.transform.localScale = Vector3.one * powerUpScale; 
        
        PowerUp script = puGo.AddComponent<PowerUp>();
        script.Setup(prefab, powerUpFloatAmplitude, type);
    }
    
    public void ClearDeferredDrops()
    {
        deferredDrops.Clear();
    }
    
    public void SpawnDebugSet(Vector3 center)
    {
        Vector3 h = Vector3.forward * 4;
        SpawnPowerUp(center + h + Vector3.up * 2, PowerUpType.Health, healthPowerUpPrefab); 
        SpawnPowerUp(center - h + Vector3.up * 2, PowerUpType.Speed, speedPowerUpPrefab);
        SpawnPowerUp(center + Vector3.left * 4 + Vector3.up * 2, PowerUpType.Shield, shieldPowerUpPrefab);
        SpawnPowerUp(center + Vector3.right * 4 + Vector3.up * 2, PowerUpType.TimeStop, timeStopPowerUpPrefab);
    }
}
