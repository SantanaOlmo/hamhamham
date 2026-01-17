using UnityEngine;
using UnityEngine.Video; // Added for Video Support

public partial class GameManager : MonoBehaviour
{
    // --- Centralized Configuration ---
    
    [Header("Player Settings")]
    public GameObject playerPrefab;
    public Vector3 playerRotationOffset;
    [Range(0.1f, 3f)] public float playerScale = 1.0f;
    public float playerVisualHeightOffset = 0f;
    
    // PROJECTILE
    public GameObject playerProjectilePrefab;
    public Vector3 playerProjectileRotationOffset;
    public float playerProjectileScale = 0.3f;
    public float playerProjectileColliderRadius = 0.15f;
    public bool showProjectileCollider = false;
    
    [Header("Player Collider Settings")]
    public bool showPlayerCollider = false;
    public ColliderType playerColliderType = ColliderType.Box;
    public Vector3 playerColliderSize = Vector3.one;
    public Vector3 playerColliderOffset = Vector3.zero;

    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    [Range(0.1f, 3f)] public float enemyScale = 0.5f;
    public float enemySpeed = 3.0f;
    public Vector3 enemyRotationOffset;
    
    [Header("Cage Settings")]
    [Range(1, 100)] public float cageAreaSize = 100f;
    [Range(1, 20)] public float cageHeight = 10f;
    [Range(0.5f, 10f)] public float cageVerticalSpacing = 5f;
    [Range(0.5f, 10f)] public float cageHorizontalSpacing = 2f;
    [Range(0.01f, 1f)] public float cageBarThickness = 0.1f;

    [Header("PowerUps (World Objects)")]
    public GameObject healthPowerUp;
    public GameObject speedPowerUp;
    public GameObject shieldPowerUp;
    public GameObject timeStopPowerUp;
    // public GameObject bombPowerUp; // Removed per user request
    public GameObject ammoPowerUp; 
    public GameObject turretPowerUp; // New
    public GameObject turretConstructPrefab; // New
    [Range(0.1f, 5.0f)] public float powerUpScale = 1.0f; 
    [Range(0.0f, 5.0f)] public float powerUpHeight = 1.0f; 
    [Range(1.0f, 10.0f)] public float shieldScale = 4.5f; 
    [Range(0.1f, 10.0f)] public float speedBoostAmount = 1.5f;
    [Range(0, 1)] public float powerUpDropRate = 0.2f;

    [Header("Environment")]
    [Range(0, 20)] public float startDelay = 7.0f;
    
    [Header("Crowd Settings")]
    [Range(1, 20)] public int crowdRows = 10;
    [Range(10, 100)] public int peoplePerRow = 50;
    [Range(0.5f, 5.0f)] public float crowdSpacing = 2.0f;
    [Range(0.1f, 5.0f)] public float crowdJumpSpeed = 2.0f;
    [Range(0.1f, 5.0f)] public float crowdRowHeightDiff = 1.0f;

    [Header("UI Icons (For HUD)")]
    public Texture2D healthIcon; 
    public Texture2D ammoIcon; 
    public Texture2D speedIcon;
    public Texture2D shieldIcon;
    public Texture2D timeStopIcon;
    public Texture2D turretIcon; // New

    [Header("UI Customization")]
    public Texture2D lifeIcon; 
    public Texture2D extraLifeIcon;
    public Texture2D bombIcon;
    public Texture2D menuBackground; 
    public Texture2D gameOverBackground; 
    public Texture2D optionsBackground; // New
    public Texture2D controlsBackground; // New
    public Texture2D titleTexture; 
    
    [Header("Video Backgrounds")] // NEW
    public VideoClip menuVideo;
    public VideoClip gameOverVideo;

    [Header("Environment Materials")]
    public Material structureMaterial; 
    public Material crowdBaseMaterial;
    public Material cageMaterial; // Added
    public Material debrisMaterial; 


    [Header("Debris Settings")]
    public PrimitiveType debrisShape = PrimitiveType.Cube;
    public Vector3 debrisScale = new Vector3(0.25f, 0.25f, 0.25f);
    public float debrisExplosionForce = 5.0f; // Low default
    public float debrisUpwardForce = 0.5f;
    public Color debrisColor = Color.gray;
}
