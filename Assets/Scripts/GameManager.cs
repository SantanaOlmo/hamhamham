using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(UIManager))]
[RequireComponent(typeof(WaveSpawner))]
public partial class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager Instance { get; private set; }

    // References
    [HideInInspector] public UIManager uiManager;
    [HideInInspector] public WaveSpawner waveSpawner;
    public PlayerController Player { get; private set; }
    
    // State Machine
    public enum GameState { MENU, PLAYING, PAUSED, GAMEOVER }
    public GameState CurrentState { get; private set; } = GameState.MENU;
    public bool ShowControlsInPause { get; set; } = false;

    // Game Data
    public int Score { get; private set; }
    public int LastScore { get; private set; }
    public int Round { get; private set; } = 1;
    public float GameTime { get; private set; }
    
    // Enemy Management
    private List<Enemy> enemies = new List<Enemy>();
    public List<Enemy> ActiveEnemies => enemies; // Public Accessor
    public int EnemyCount => enemies.Count;
    
    // Round Progress
    public int TotalEnemiesThisRound = 0;
    public int EnemiesKilledThisRound = 0;

    // Special Ability (Bomb)
    public int bombStoredCount = 0;
    public bool isSpecialAbilityAvailable => bombStoredCount > 0; // Backward compat property
    
    // Time Stop Inventory
    public int timeStopStoredCount = 0;
    public bool HasTimeStopStored => timeStopStoredCount > 0; // Backward compat property
    public bool IsTimeFrozen { get; private set; } = false;
    
    [Header("Power-Up Limits")]
    public int maxSpeedUpgrades = 10;
    public int maxShieldCharges = 5;
    public int maxTurretStacks = 5;
    public int maxBombStacks = 5;
    public int maxTimeStopStacks = 5;

    [Header("UI Config")]
    public Color uiCounterColor = Color.cyan; // Customizable color for xN counters

    // Wave Management
    private bool waitingForWave = false; 
    public Light mainLight; // Cached reference
    
    private bool scoreSubmitted = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        // Bootstrap the GameManager
        if (FindFirstObjectByType<GameManager>() == null)
        {
            GameObject go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
            DontDestroyOnLoad(go);
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 
        
        // Force Standard Resolution (16:9)
        // Force Standard Resolution (16:9)
        // Screen.SetResolution(1280, 720, FullScreenMode.Windowed); // Disabled to allow Fullscreen/Maximize logic by user 

        // Load scores on startup (Method in Resources partial)
        LoadHighScores(); 

        // Auto-Setup ObjectPoolManager
        if (ObjectPoolManager.Instance == null)
        {
            GameObject poolGo = new GameObject("ObjectPoolManager");
            poolGo.AddComponent<ObjectPoolManager>();
            poolGo.transform.SetParent(transform);
        }

        // Auto-Setup SoundManager
        if (SoundManager.Instance == null)
        {
             GameObject soundGo = new GameObject("SoundManager");
             soundGo.AddComponent<SoundManager>();
             soundGo.transform.SetParent(transform);
        }
        
        // Find managers
        if (uiManager == null) uiManager = GetComponent<UIManager>();
        if (uiManager == null) uiManager = gameObject.AddComponent<UIManager>();
        
        if (waveSpawner == null) waveSpawner = GetComponent<WaveSpawner>();
        if (waveSpawner == null) waveSpawner = gameObject.AddComponent<WaveSpawner>();

        // --- Inject Configuration into Managers (Fields in Config partial) ---
        
        // 1. Configure Spawner
        waveSpawner.enemyModel = this.enemyPrefab;
        waveSpawner.enemyScale = this.enemyScale;
        waveSpawner.enemyBaseSpeed = this.enemySpeed;
        waveSpawner.enemyRotationOffset = this.enemyRotationOffset;
        
        // Pass Prefabs for World Spawning
        waveSpawner.healthPowerUpPrefab = this.healthPowerUp;
        waveSpawner.speedPowerUpPrefab = this.speedPowerUp;
        waveSpawner.shieldPowerUpPrefab = this.shieldPowerUp;
        waveSpawner.timeStopPowerUpPrefab = this.timeStopPowerUp;
        waveSpawner.ammoPowerUpPrefab = this.ammoPowerUp; 
        waveSpawner.turretPowerUpPrefab = this.turretPowerUp; // New
        
        waveSpawner.powerUpScale = this.powerUpScale;
        waveSpawner.powerUpBaseHeight = this.powerUpHeight; 
        waveSpawner.powerUpDropRate = this.powerUpDropRate;

        // 2. Configure UI
        uiManager.lifeIcon = this.lifeIcon != null ? this.lifeIcon : this.healthIcon; 
        uiManager.extraLifeIcon = this.extraLifeIcon; 
        uiManager.bombIcon = this.bombIcon;
        uiManager.bombIcon = this.bombIcon;
        // uiManager.menuBackground & gameOverBackground now handled dynamically in UIManager via GM Instance
        uiManager.optionsBackground = this.optionsBackground; 
        uiManager.controlsBackground = this.controlsBackground;
        uiManager.uiButtonTexture = this.uiButtonTexture;
        Debug.Log($"[GameManager Awake] Assigned uiButtonTexture: {(uiButtonTexture != null ? uiButtonTexture.name : "NULL")}");
        
        uiManager.topScorersBoxTexture = this.topScorersBoxTexture; 
        Debug.Log($"[GameManager Awake] Assigned topScorersBoxTexture: {(topScorersBoxTexture != null ? topScorersBoxTexture.name : "NULL")}");
        
        uiManager.controlsBackground = this.controlsBackground;
        
        // Use the Icon fields for UI status
        uiManager.shieldIcon = this.shieldIcon;
        uiManager.speedIcon = this.speedIcon;
        uiManager.timeStopIcon = this.timeStopIcon;
        uiManager.ammoIcon = this.ammoIcon; 
        uiManager.turretIcon = this.turretIcon; // New 
        
        if (this.titleTexture != null) uiManager.titleTexture = this.titleTexture;
    }

    void Start()
    {
        // Re-inject UI Textures to be absolutely sure they are present
        if (uiManager != null)
        {
            uiManager.uiButtonTexture = this.uiButtonTexture;
            uiManager.topScorersBoxTexture = this.topScorersBoxTexture;
            uiManager.optionsBackground = this.optionsBackground;
            uiManager.controlsBackground = this.controlsBackground;
        }

        if (mainLight == null) mainLight = FindFirstObjectByType<Light>();
        StartCoroutine(LoadResources()); // In Resources partial
    }

    // Power-Up Selection
    public int selectedSlotIndex { get; private set; } = 0; // 0-4

    // Turret Inventory
    public int turretStoredCount = 0;
    public bool HasTurretStored => turretStoredCount > 0; // Backward compat property
    
    // Turret Preview State
    private GameObject turretPreviewInstance;
    private bool isPlacementValid = false;
    public float arenaSize = 45f; // Safe zone size

    void Update()
    {
        HandleSlotSelection(); // Moved here to be always active for scrolling

        if (CurrentState == GameState.PLAYING)
        {
            UpdateTurretPreview();
        }

        // Pause
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (CurrentState == GameState.PLAYING || CurrentState == GameState.PAUSED) TogglePause(); // In State partial
        }





        if (CurrentState == GameState.PLAYING)
        {
            HandleContextualInput(); // Moved here
            if (!IsTimeFrozen) GameTime += Time.deltaTime;
            
            // Check Wave
            if (enemies.Count == 0 && waveSpawner != null && !waitingForWave)
            {
                 StartCoroutine(PrepareNextWave()); // In Combat partial
            }
            
            // Cheats
            if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame) GameOver(); // In State partial

            // DISCO MODE SYNC (In Disco partial)
            SyncDiscoMode(); 
        }
    }

    void UpdateTurretPreview()
    {
        // 1. Check if Turret Slot is Selected (Index 4) AND we have one stored
        bool shouldShow = (selectedSlotIndex == 4 && HasTurretStored);

        if (shouldShow)
        {
            // Position: Right of player
            if (Player == null) return;
            Vector3 targetPos = Player.transform.position + Player.transform.right * 2f;
            targetPos.y = Player.transform.position.y; // Keep same height logic

            // 2. Validate Bounds
            bool inBoundsX = Mathf.Abs(targetPos.x) < arenaSize;
            bool inBoundsZ = Mathf.Abs(targetPos.z) < arenaSize;
            isPlacementValid = inBoundsX && inBoundsZ;

            // 3. Create Preview if needed
            if (turretPreviewInstance == null)
            {
                if (this.turretConstructPrefab != null)
                {
                    turretPreviewInstance = Instantiate(this.turretConstructPrefab);
                    turretPreviewInstance.name = "Turret_Preview";
                    
                    // Strip functionality
                    Destroy(turretPreviewInstance.GetComponent<Turret>());
                    Destroy(turretPreviewInstance.GetComponent<Collider>()); // No physical interactions
                    
                    // Recursive cleanup of colliders in children
                    foreach(var col in turretPreviewInstance.GetComponentsInChildren<Collider>()) Destroy(col);
                }
            }

            // 4. Update Position & Visuals
            if (turretPreviewInstance != null)
            {
                turretPreviewInstance.transform.position = targetPos;
                // Match Player rotation to feel like a "connected" side object
                turretPreviewInstance.transform.rotation = Player.transform.rotation;

                Color color = isPlacementValid ? Color.blue : Color.red;
                
                // Recursively colorize
                Renderer[] rends = turretPreviewInstance.GetComponentsInChildren<Renderer>();
                foreach(var r in rends)
                {
                    r.material.color = color;
                    if (r.material.HasProperty("_EmissionColor"))
                    {
                         r.material.SetColor("_EmissionColor", color * 2f); 
                         r.material.EnableKeyword("_EMISSION");
                    }
                }
                 
                turretPreviewInstance.SetActive(true);
            }
        }
        else
        {
            // Hide if not selected or invalid
            if (turretPreviewInstance != null) turretPreviewInstance.SetActive(false);
        }
    }

    void HandleSlotSelection()
    {
        if (Mouse.current == null) return;
        
        float scroll = Mouse.current.scroll.y.ReadValue();
        if (scroll > 0)
        {
            selectedSlotIndex--;
            if (selectedSlotIndex < 0) selectedSlotIndex = 5;
        }
        else if (scroll < 0)
        {
            selectedSlotIndex++;
            if (selectedSlotIndex > 5) selectedSlotIndex = 0;
        }
    }



    // ... (Existing HandleSlotSelection logic) ...

    void HandleContextualInput()
    {
        if (Mouse.current == null) return;

        // Slot Configuration:
        // 0: Speed 
        // 1: Shield
        // 2: Time Stop (Right Click)
        // 3: Ammo
        // 4: Turret (Right Click)
        // 5: Bomb (Right Click)

        // Time Stop (Slot 2) - Right Click (CHANGED from Left)
        if (selectedSlotIndex == 2 && HasTimeStopStored)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                ActivateTimeStop();
            }
        }

        // Turret (Slot 4) - Right Click
        if (selectedSlotIndex == 4 && HasTurretStored)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                DeployTurret();
            }
        }

        // Bomb (Slot 5) - Right Click
        if (selectedSlotIndex == 5 && isSpecialAbilityAvailable)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
               StartCoroutine(ActivateSpecialAbility());
            }
        }
    }

    public void StoreTurret()
    {
        turretStoredCount++;
        // SFX?
    }

    public void DeployTurret()
    {
        if (!HasTurretStored) return;
        if (Player == null) return;
        
        // Validation Check
        if (!isPlacementValid)
        {
            // Maybe play a "Denied" sound?
            // for now, just return
            return;
        }

        // Instantiate Turret at Preview Position (calculated same way)
        Vector3 spawnPos = Player.transform.position + Player.transform.right * 2f;
        spawnPos.y = Player.transform.position.y;

        if (this.turretConstructPrefab != null)
        {
             Instantiate(this.turretConstructPrefab, spawnPos, Player.transform.rotation);
             turretStoredCount--; 
             
             if (SoundManager.Instance != null && SoundManager.Instance.sfxTurretPlace != null)
             {
                 SoundManager.Instance.PlaySFX(SoundManager.Instance.sfxTurretPlace);
             } 
             // SFX?
        }
        else
        {
            Debug.LogError("Turret Construct Prefab is NULL in GameManager!");
        }
    }
}
