using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public partial class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance;

    [Header("Enemy Assets (Managed by GameManager)")]
    [HideInInspector] public GameObject enemyModel;
    [HideInInspector] public RuntimeAnimatorController enemyAnimatorController;
    [HideInInspector] public Vector3 enemyRotationOffset = new Vector3(0, 0, 0);

    [Header("Enemy Config (Managed by GameManager)")]
    [HideInInspector] public float enemyScale = 1.6f;
    [HideInInspector] public float enemyBaseSpeed = 3.0f;
    [HideInInspector] public float enemySpeedIncreasePerRound = 0.1f;
    
    [Header("Wave Settings")]
    public int enemiesPerWave = 5;
    public int additionalEnemiesPerWave = 5;

    [Header("PowerUp Config (Managed by GameManager)")]
    [HideInInspector] public GameObject healthPowerUpPrefab;
    [HideInInspector] public GameObject speedPowerUpPrefab;
    [HideInInspector] public GameObject shieldPowerUpPrefab;
    [HideInInspector] public GameObject timeStopPowerUpPrefab;
    [HideInInspector] public GameObject ammoPowerUpPrefab; 
    [HideInInspector] public GameObject turretPowerUpPrefab; // New

    [HideInInspector] public float powerUpDropRate;
    
    [Range(0.01f, 3.0f)] public float powerUpScale = 1.0f; 
    [HideInInspector] public float powerUpBaseHeight = 1.0f; 
    [Range(0.0f, 2.0f)] public float powerUpFloatAmplitude = 0.5f;

    [Header("PowerUp Rates")]
    public int healthSpawnRate = 20;
    public int speedSpawnRate = 30;
    public int shieldSpawnRate = 45;
    public int timeStopSpawnRate = 60;
    public int turretSpawnRate = 75; // New
    
    [Header("Enemy Data (ScriptableObjects)")]
    public EnemyData normalData;
    // public EnemyData tigerData; // Removed as per user request (consolidated into normalData)
    public EnemyData fireTigerData; 
    public EnemyData bossData;
    
    [System.Serializable]
    public struct WaveComponent 
    { 
        public EnemyData enemyData; 
        public int count; 
    }
    
    [System.Serializable]
    public struct SpecialRound 
    { 
        public int roundNumber; 
        public List<WaveComponent> waves; 
    }

    [Header("Special Rounds Config")]
    public List<SpecialRound> specialRounds = new List<SpecialRound>();
    
    private GameManager gm;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        gm = GameManager.Instance;
        if (fireTigerData == null) Debug.LogError("WaveSpawner: FireTigerData is NOT assigned in the Inspector! Fire Tigers will default to Normal or fail.");
    }
}
