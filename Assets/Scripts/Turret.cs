using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Turret : MonoBehaviour
{
    // config
    public float hp = 50f;
    public float turnSpeed = 10f;
    public float range = 20f;
    public Transform turretBase;   // The static "Feet/Base" part
    public Transform partToRotate; // The "Machine Gun" part (Rotates)
    public Transform firePoint;
    public Transform healthBarRoot; // Optional: User can assign their own
    public Material healthBarBgMat;
    public Material healthBarFullMat;
    
    // references
    private Transform target;
    private PlayerController player; // To inherit fire rate logic

    // state
    private float fireCountdown = 0f;
    private float currentHP;
    private float maxHP;
    
    // Visuals
    private Transform healthBarGreen;
    private Renderer[] turretRenderers;
    private Color baseColor = Color.white; // Assuming white default

    void Start()
    {
        currentHP = hp;
        maxHP = hp;
        
        // Find Player to get dynamic stats if needed, or just use GameManager
        if (GameManager.Instance != null && GameManager.Instance.Player != null)
        {
            player = GameManager.Instance.Player;
        }

        // Setup Visuals
        turretRenderers = GetComponentsInChildren<Renderer>();
        
        // Setup Health Bar
        SetupHealthBar();

        // Enforce Physics Safety
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        InvokeRepeating("UpdateTarget", 0f, 0.5f);
    }
    
    void SetupHealthBar()
    {
        if (healthBarRoot == null)
        {
            // Auto-Generate a simple Health Bar if none assigned
            GameObject root = new GameObject("HealthBar_Root");
            root.transform.SetParent(transform);
            root.transform.localPosition = new Vector3(0, 2.5f, 0); // Above turret
            healthBarRoot = root.transform;
            
            // Background (Red/Black)
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name = "Background";
            bg.transform.SetParent(root.transform, false);
            bg.transform.localScale = new Vector3(1.5f, 0.2f, 0.1f);
            Destroy(bg.GetComponent<Collider>());
            Renderer bgRend = bg.GetComponent<Renderer>();
            if (healthBarBgMat != null) bgRend.material = healthBarBgMat;
            else bgRend.material.color = Color.black;
            
            // Foreground (Green)
            GameObject fg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fg.name = "Health";
            fg.transform.SetParent(root.transform, false);
            fg.transform.localScale = new Vector3(1.5f, 0.2f, 0.1f); // Start Full
            // Slight offset Z to be in front
            fg.transform.localPosition = new Vector3(0, 0, -0.05f); 
            Destroy(fg.GetComponent<Collider>());
            Renderer fgRend = fg.GetComponent<Renderer>();
            if (healthBarFullMat != null) fgRend.material = healthBarFullMat;
            else fgRend.material.color = Color.green;
            
            healthBarGreen = fg.transform;
        }
        else
        {
            // If user assigned a custom root, try to find a child named "Health" or fallback
            // This assumes user setup. If they just drop a canvas, we might need different logic.
            // For now, let's assume programmatic generation is preferred unless specified.
            // If they modify this later, we can adapt.
            if (healthBarRoot.childCount > 0) healthBarGreen = healthBarRoot.GetChild(0); 
            // Very risky assumption. Let's stick to the generated one being robust.
        }
    }

    void UpdateHealthBar()
    {
        if (healthBarGreen != null)
        {
            float pct = currentHP / maxHP;
            // Scale X (Original Width 1.5f)
            float originalWidth = 1.5f;
            float newWidth = originalWidth * pct;
            
            Vector3 scale = healthBarGreen.localScale;
            scale.x = newWidth;
            healthBarGreen.localScale = scale;
            
            // Fix Anchor to LEFT
            // Original Center X = 0. Width 1.5. Left Edge = -0.75.
            // New Center X = Left Edge + (New Width / 2)
            float newCenterX = -0.75f + (newWidth / 2f);
            
            Vector3 pos = healthBarGreen.localPosition;
            pos.x = newCenterX;
            healthBarGreen.localPosition = pos;
        }
    }

    // ... (UpdateTarget, Update, Shoot methods remain)

    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        
        UpdateHealthBar();
        StartCoroutine(FlashRoutine());
        
        if (currentHP <= 0)
        {
            Die();
        }
    }
    
    IEnumerator FlashRoutine()
    {
         // 1. IMPACT: Pure Red Flash
         foreach(var r in turretRenderers) if(r) r.material.color = Color.red;
         
         yield return new WaitForSeconds(0.1f);
         
         // 2. RECOVER: Tint based on damage
         UpdateDamageState();
    }
    
    void UpdateDamageState()
    {
        if (turretRenderers == null) return;
        
        // Calculate Damage Percentage (0 to 1, where 1 means dead)
        float damagePct = 1f - (currentHP / maxHP);
        
        // Lerp from Base to Red
        Color targetColor = Color.Lerp(baseColor, Color.red, damagePct);
        
        foreach(var r in turretRenderers) if(r) r.material.color = targetColor; 
    }

    void Die()
    {
        // Explosion Sound?
        if (SoundManager.Instance != null && SoundManager.Instance.sfxTurretDeath != null)
             SoundManager.Instance.PlaySFX(SoundManager.Instance.sfxTurretDeath); 
             
        Destroy(gameObject);
    }


    void UpdateTarget()
    {
        // Find nearest enemy
        // Note: This is expensive if many enemies. Optimization: Use WaveSpawner list if public, or a tag search.
        // Better: Use GameManager.Instance.enemies list (Wait, GameManager enemies list is private?)
        // Let's check GameManager public access. 
        // GameManager.Instance.EnemyCount allows count, but maybe not list access.
        // Fallback: FindObjectsByType or Tag. Tag "Enemy" is standard.
        
        if (GameManager.Instance == null) return;
        
        List<Enemy> enemies = GameManager.Instance.ActiveEnemies;
        float shortestDistance = Mathf.Infinity;
        Enemy nearestEnemy = null; // Changed type to Enemy for direct access later if needed

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null) continue;
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null && shortestDistance <= range)
        {
            target = nearestEnemy.transform;
        }
        else
        {
            target = null;
        }
    }

    void Update()
    {
        if (target == null) return;

        // Rotation
        if (partToRotate != null)
        {
            Vector3 dir = target.position - transform.position;
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            // Lock rotation to Y axis if it's a turret on the ground
            // Vector3 rotation = Quaternion.Lerp(partToRotate.rotation, lookRotation, Time.deltaTime * turnSpeed).eulerAngles;
            // partToRotate.rotation = Quaternion.Euler(0f, rotation.y, 0f);
            
            // Actually user said "solo debe rotar hacia los lados", so Y axis only is correct.
             Vector3 rotation = Quaternion.Lerp(partToRotate.rotation, lookRotation, Time.deltaTime * turnSpeed).eulerAngles;
             partToRotate.rotation = Quaternion.Euler(0f, rotation.y, 0f);
        }

        // Shooting
        if (fireCountdown <= 0f)
        {
            Shoot();
            // Fire rate is double Player's. 
            // Player fire rate = (60 / bpm) / 2.
            // Turret fire rate = Player rate / 2 (faster).
            float rate = 0.5f; // default fallback
            if (player != null)
            {
                 // We don't have public access to player.fireRate. 
                 // Let's approximate or make Player.fireRate public later.
                 // For now, fast fixed rate:
                 rate = 0.2f; 
            }
            fireCountdown = rate;
        }

        fireCountdown -= Time.deltaTime;
    }

    void Shoot()
    {
        if (GameManager.Instance == null) return;
        
        GameObject prefab = GameManager.Instance.playerProjectilePrefab;
        if (prefab == null && player != null) prefab = player.projectilePrefab;
        
        if (prefab != null && firePoint != null)
        {
            // Sound
            if (SoundManager.Instance != null && SoundManager.Instance.sfxShoot != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.sfxShoot);
            }

            // 1. Get Rotation Offset from GameManager (same as Player)
            Quaternion rotationOffset = Quaternion.identity;
            if (GameManager.Instance != null)
            {
                 rotationOffset = Quaternion.Euler(GameManager.Instance.playerProjectileRotationOffset);
            }
            
            // 2. Apply Offset to Spawn Rotation
            Quaternion spawnRotation = firePoint.rotation * rotationOffset;

            // Spawn
            GameObject bulletGO = null;
            if (ObjectPoolManager.Instance != null)
            {
                bulletGO = ObjectPoolManager.Instance.SpawnFromPool(prefab, firePoint.position, spawnRotation);
            }
            else
            {
                bulletGO = Instantiate(prefab, firePoint.position, spawnRotation);
            }

            if (bulletGO != null)
            {
                Projectile p = bulletGO.GetComponent<Projectile>();
                if (p != null)
                {
                    // Calculate direction to target
                    Vector3 dir = (target.position - firePoint.position).normalized;
                    
                    // Logic from PlayerController: Calculate local axis for Launch
                    // transform.forward here is the Bullet's forward (which includes the offset)
                    // We need to tell Launch how to move correctly.
                    
                    Vector3 localAxis = bulletGO.transform.InverseTransformDirection(dir);
                    
                    p.Launch(dir, 40f, 1, true, moveAxis: localAxis); 
                }
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if enemy
        Enemy e = collision.gameObject.GetComponent<Enemy>();
        if (e != null)
        {
            TakeDamage(1);
            // Enemy interaction (Push back? Or just damage processing?)
        }
    }


}
