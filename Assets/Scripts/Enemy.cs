using UnityEngine;

public partial class Enemy : MonoBehaviour
{
    // References
    private GameManager gm;
    private Transform target;
    private Rigidbody rb;
    private Animator anim; // Assuming usage
    
    // Data
    private EnemyData data;
    public EnemyData Data => data; 

    // Stats
    private float moveSpeed;
    public bool IsBoss { get; private set; }
    private int maxHealth;
    private Color baseColor = Color.white;
    
    // Setup Method
    public void Setup(GameManager manager, Transform playerTransform, EnemyData enemyData, float speed, int health, bool boss, int round)
    {
        this.gm = manager;
        this.target = playerTransform;
        this.data = enemyData;
        
        this.moveSpeed = speed;
        this.currentHealth = health;
        this.maxHealth = health; // Store Max Health
        this.IsBoss = boss;

        // Determine Base Color
        if (IsBoss)
        {
            this.baseColor = Color.red;
            transform.localScale *= 1.5f; 
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.material.color = this.baseColor;
            }
        }
        else if (data != null && data.type == EnemyType.Shooter)
        {
             // Shooter enemies are black
             this.baseColor = Color.black;
             Renderer[] renderers = GetComponentsInChildren<Renderer>();
             if (renderers.Length == 0) Debug.LogWarning($"Enemy {name} (Shooter) has NO Renderers in children!");
             foreach (Renderer r in renderers)
             {
                 r.material.color = this.baseColor;
             }
        }
        else
        {
            // Normal enemies default to white (or whatever was set by prefab/others)
            this.baseColor = Color.white; 
            // Optional: Force apply if needed, but let's assume prefab default is okay or set here.
             Renderer[] renderers = GetComponentsInChildren<Renderer>();
             foreach (Renderer r in renderers)
             {
                 // Check if it's already colored? 
                 // For now, assume White base for calculation.
                 // If the prefab is Red, this might act weird. 
                 // Let's explicitly set them to white if they aren't special types, 
                 // just to ensure our tint logic starts from a known state.
                 r.material.color = this.baseColor;
             }
        }
        
        Debug.Log($"Enemy Setup: {name}. Type: {(data != null ? data.type.ToString() : "Null Data")}. Boss: {boss}.");
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
    }
}
