using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "ScriptableObjects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Base Stats")]
    public string enemyName = "Enemy";
    public float baseSpeed = 3.0f;
    public int baseHealth = 1;
    public int damage = 1;
    public int scoreValue = 1;

    [Header("Difficulty Scaling")]
    public float speedGrowthPerRound = 0.1f;
    public int healthGrowthPerRound = 0; // e.g. 1 per 5 rounds = 0.2? Integer math is safer: "Every X Rounds" logic in Spawner is better, or just Base + Round/X
    public int healthBonusPer5Rounds = 1;

    [Header("Visuals")]
    public float modelScale = 1.6f;
    public GameObject prefabOverride; // If specific prefab needed (e.g. Tiger)
    public RuntimeAnimatorController animatorOverride; 

    [Header("Behavior")]
    public EnemyType type = EnemyType.Melee;
    public float attackRange = 10.0f;
    public float fireRate = 1.5f; // Seconds between shots
    public GameObject projectilePrefab;
    public Vector3 projectileRotationOffset; // Added for adjusting projectile orientation
    [Header("Collider Settings")]
    public float visualHeightOffset = 0f; // New Visual Height field
    public bool showCollider = false;
    public ColliderType colliderType = ColliderType.Box;
    public Vector3 colliderSize = Vector3.one;
    public Vector3 colliderOffset = Vector3.zero;
}

public enum EnemyType
{
    Normal,
    Melee,
    Shooter
}

public enum ColliderType
{
    Box,
    Sphere
}
