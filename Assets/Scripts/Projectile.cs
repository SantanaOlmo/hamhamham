using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 2f;
    private int damage = 1;
    private bool isPlayerProjectile = true;
    private float timer;
    private GameObject debugColliderVisual; // Debug Visual Reference

    void Awake()
    {
        // One-time Setup if visual needed
        if (transform.childCount == 0 && GetComponent<Renderer>() == null)
        {
             CreateVisuals();
        }
        
        // Ensure Physics
        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null)
        {
             col = gameObject.AddComponent<SphereCollider>();
             col.radius = 0.15f;
             col.isTrigger = true;
        }

        // CRITICAL FIX: Ensure Rigidbody exists for Trigger events to work on static colliders (like Turret)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure RB for Projectile (Kinematic/No Gravity so we control movement via Translate)
        rb.useGravity = false;
        rb.isKinematic = true; 

        defaultRadius = col.radius;
        defaultCenter = col.center;
    }

    private float defaultRadius;
    private Vector3 defaultCenter;

    void CreateVisuals()
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = Vector3.one * 0.3f;
        Destroy(visual.GetComponent<SphereCollider>());
        
        Renderer rend = visual.GetComponent<Renderer>();
        if (rend != null)
        {
             // Apply Base Material if available
             // (User removed bulletMaterial from GM)
             rend.material.color = Color.yellow; 
             rend.material.EnableKeyword("_EMISSION");
             rend.material.SetColor("_EmissionColor", Color.yellow * 3.0f);
        }
    }

    private Vector3 moveDirectionWorld; // Store World Direction directly
    public Material customMaterial; // User can assign in Prefab

    public void Launch(Vector3 direction, float speed, int damage, bool isPlayerProjectile, Vector3? moveAxis = null, float? lifeTimeOverride = null)
    {
        // Use the passed world direction as the source of truth for movement
        moveDirectionWorld = direction.normalized;
        
        // Visual Rotation: Face the direction (optional, but good for visuals)
        if (moveAxis == null) this.transform.forward = moveDirectionWorld;
        
        this.speed = speed;
        this.damage = damage;
        this.isPlayerProjectile = isPlayerProjectile; 
        this.timer = lifeTimeOverride.HasValue ? lifeTimeOverride.Value : lifeTime;

        // Force reset scale to 1 ONLY for player to ensure config applies cleanly.
        // For enemies, respect the Prefab's scale.
        if (isPlayerProjectile)
        {
            transform.localScale = Vector3.one;
        }

        if (GameManager.Instance != null)
        {
             Debug.Log($"[Proj Launch] GM Found. configScale: {GameManager.Instance.playerProjectileScale}, configRad: {GameManager.Instance.playerProjectileColliderRadius}. Dir: {moveDirectionWorld}");
        }
        else
        {
             Debug.LogError("[Proj Launch] GM Missing!");
        }

        // Dynamic Scale Adjustment for Enemies (User requested 9x smaller collider)
        if (!isPlayerProjectile)
        {
             SphereCollider col = GetComponent<SphereCollider>();
             if (col != null)
             {
                 col.radius = 0.02f; 
                 col.center = Vector3.zero;
             }
        }
        else
        {
             // PLAYER PROJECTILE CONFIGURATION
             if (GameManager.Instance != null)
             {
                 // 1. Apply Prefab Scale (Visual Size)
                 transform.localScale = Vector3.one * GameManager.Instance.playerProjectileScale;
                 
                 SphereCollider col = GetComponent<SphereCollider>();
                 if (col != null)
                 {
                     col.center = Vector3.zero; 
                     // 2. Apply Collider Radius
                     col.radius = GameManager.Instance.playerProjectileColliderRadius;
                     
                     // 3. Handle Debug Visibility
                     if (GameManager.Instance.showProjectileCollider)
                     {
                         if (debugColliderVisual == null)
                         {
                             debugColliderVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                             debugColliderVisual.name = "DEBUG_COLLIDER";
                             debugColliderVisual.transform.SetParent(transform, false);
                             Destroy(debugColliderVisual.GetComponent<Collider>());
                             
                             // Material (Red transparent)
                             Renderer r = debugColliderVisual.GetComponent<Renderer>();
                             if (r != null)
                             {
                                 r.material = new Material(Shader.Find("Sprites/Default"));
                                 r.material.color = new Color(1f, 0f, 0f, 0.4f);
                             }
                         }
                         
                         debugColliderVisual.SetActive(true);
                         // Match collider size: Sphere primitive has radius 0.5 (diameter 1).
                         // To match collider radius R, scale should be R * 2.
                         debugColliderVisual.transform.localScale = Vector3.one * (col.radius * 2f);
                     }
                     else
                     {
                         if (debugColliderVisual != null) debugColliderVisual.SetActive(false);
                     }
                 }
             }
             else
             {
                 // Fallback if GM is missing
                 SphereCollider col = GetComponent<SphereCollider>();
                 if (col != null)
                 {
                     col.center = Vector3.zero;
                     col.radius = 0.15f;
                 }
             }
        }

        UpdateVisualsColor();
    }

    public bool overrideColor = true;

    void UpdateVisualsColor()
    {
        if (!overrideColor) return; 

        Renderer[] r = GetComponentsInChildren<Renderer>();
        foreach(var rend in r)
        {
             if (customMaterial != null)
             {
                 rend.material = customMaterial;
             }
             else
             {
                 if (!isPlayerProjectile) 
                 {
                     rend.material.color = Color.magenta;
                     if (rend.material.HasProperty("_EmissionColor"))
                        rend.material.SetColor("_EmissionColor", Color.magenta * 3.0f);
                 }
             }
        }
    }

    void OnEnable()
    {
        isPlayerProjectile = false; 
    }

    void Update()
    {
        // Debug movement
        if (timer > lifeTime - 0.1f) // Log only first few frames relative to lifetime
        {
             // Debug.Log($"[Proj Frame] Pos: {transform.position}, Dir: {moveDirectionWorld}, Speed: {speed}, Scale: {transform.localScale}");
        }

        // Pure World Space Movement
        Vector3 previousPos = transform.position;
        transform.Translate(moveDirectionWorld * speed * Time.deltaTime, Space.World);
        
        if (Vector3.Distance(previousPos, transform.position) < 0.0001f && speed > 0)
        {
             // Debug.LogWarning($"[Proj Stuck] Should move but didn't! Speed: {speed}, Dir: {moveDirectionWorld}");
        }
        
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Remove();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // DEBUG: Diagnose why collisions aren't registering or killing
        Debug.Log($"[Projectile Debug] Triggered with {other.gameObject.name}. IsPlayer: {isPlayerProjectile}. Layer: {other.gameObject.layer}");

        if (isPlayerProjectile)
        {
            // Use GetComponentInParent in case we hit a child collider (Visual mesh)
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                Debug.Log($"Player Projectile hit Enemy: {enemy.name}");
                enemy.TakeDamage(damage); 
                Remove();
            }
        }
        else // Enemy Projectile
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                Debug.Log($"Enemy Projectile hit Player: {player.name}");
                if (!player.IsInvulnerable)
                {
                    player.TakeDamage(damage);
                    Remove();
                }
            }
            else
            {
                // Check for Turret (New)
                Turret turret = other.GetComponentInParent<Turret>();
                if (turret != null)
                {
                     Debug.Log($"Enemy Projectile hit Turret: {turret.name}");
                     turret.TakeDamage(damage);
                     Remove();
                }
            }
        }
    }

    void Remove()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(this.gameObject, this.gameObject.name.Replace("(Clone)", "").Trim());
        }
        else
        {
            Destroy(gameObject);
        }
    }


}
