using UnityEngine;
using System.Collections;

public partial class Enemy : MonoBehaviour
{
    // State
    private bool isFrozen = false;
    private float fireRate = 2.0f;
    private float lastShotTime;
    
    // Combat
    private int currentHealth;

    public void Freeze(bool state)
    {
        isFrozen = state;
        if (state)
        {
            if (rb != null) rb.linearVelocity = Vector3.zero;
            if (anim != null) anim.speed = 0;
        }
        else
        {
            if (anim != null) anim.speed = 1;
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        
        // Flash Effect
        StartCoroutine(FlashRoutine());
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator FlashRoutine()
    {
         var rends = GetComponentsInChildren<Renderer>();
         
         // 1. IMPACT: Pure Red Flash (Max Intensity)
         foreach(var r in rends) r.material.color = Color.red;
         
         yield return new WaitForSeconds(0.1f);
         
         // 2. RECOVER: Return to Base Color + Health Tint
         UpdateDamageState(rends);
    }

    void UpdateDamageState(Renderer[] rends = null)
    {
        if (rends == null) rends = GetComponentsInChildren<Renderer>();
        
        // Calculate Damage Percentage (0 to 1, where 1 means dead)
        float damagePct = 1f - ((float)currentHealth / (float)maxHealth);
        
        // Target color: Lerp from Base to Red based on damage
        Color targetColor = Color.Lerp(baseColor, Color.red, damagePct);
        
        foreach(var r in rends) r.material.color = targetColor; 
    }

    public void Die()
    {
        if (gm != null) gm.OnEnemyKilled(this);
        
        // Spawn Debris
        if (ObjectPoolManager.Instance != null && GameManager.Instance != null && GameManager.Instance.debrisMaterial != null)
        {
             // Spawn 4-5 cubes
             // Spawn 4-5 debris chunks
             for(int i=0; i<5; i++)
             {
                 // Use Shape from Config
                 PrimitiveType shape = GameManager.Instance.debrisShape;
                 GameObject d = GameObject.CreatePrimitive(shape);
                 
                 d.transform.position = transform.position + Random.insideUnitSphere;
                 
                 // Use Scale from Config
                 d.transform.localScale = GameManager.Instance.debrisScale;
                 
                 Renderer r = d.GetComponent<Renderer>();
                 if (r != null)
                 {
                     if (GameManager.Instance.debrisMaterial != null)
                         r.material = GameManager.Instance.debrisMaterial;
                     
                     // Helper: Apply color if using default material or compatible shader
                     r.material.color = GameManager.Instance.debrisColor;
                 }
                 
                 d.AddComponent<Debris>(); 
             }
        }
        
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
            if (pc != null)
            {
                if (IsBoss) pc.TakeDamage(100); // Instant Kill
                else pc.TakeDamage(1); 
                
                // IMPACT FIX:
                // Removed all recoil/pushback. 
                // Checks confirm user wants enemies to be "glued" to the player when attacking.
                // Any force here causes a push-shoot-push loop that looks like they stop moving.
                
                // Zero out velocity to prevent drift
                if (rb != null) rb.linearVelocity = Vector3.zero;
            }
        }
    }

    void FireBurst()
    {
        // 3 shot burst
        StartCoroutine(BurstRoutine());
    }

    IEnumerator BurstRoutine()
    {
        Debug.Log($"[Enemy {name}] BurstRoutine Started. Round: {(GameManager.Instance != null ? GameManager.Instance.Round : -1)}");
        // Determine burst size based on Round
        int burstCount = 1;
        if (GameManager.Instance != null && GameManager.Instance.Round >= 14)
        {
            burstCount = 3;
        }

        for (int i=0; i<burstCount; i++)
        {
            GameObject p = null;
            
            // ... (Instantiation Logic)
            if (Data != null && Data.projectilePrefab != null)
            {
                 Quaternion rotation = transform.rotation * Quaternion.Euler(Data.projectileRotationOffset);
                 p = Instantiate(Data.projectilePrefab, transform.position + transform.forward, rotation);
                 Debug.Log($"[Enemy {name}] Instantiated from Data Prefab: {Data.projectilePrefab.name}");
            }
            else
            {
                 // Fallback
                 p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                 p.transform.position = transform.position + transform.forward;
                 p.transform.rotation = transform.rotation;
                 p.transform.localScale = Vector3.one * 0.3f;
                 
                 // Apply Material manually
                 Renderer r = p.GetComponent<Renderer>();
                 if (r != null)
                 {
                      r.material = new Material(Shader.Find("Sprites/Default")); // Fallback standard
                      r.material.color = Color.red;
                 }
                     
                 if (p.GetComponent<Collider>() == null) p.AddComponent<SphereCollider>();
                 Debug.Log($"[Enemy {name}] Created Primitive Fallback");
            }

            if (p != null)
            {
                Projectile proj = p.GetComponent<Projectile>();
                if (proj == null) 
                {
                    proj = p.AddComponent<Projectile>();
                    if (Data != null && Data.projectilePrefab != null) proj.overrideColor = false;
                    Debug.Log($"[Enemy {name}] Added Projectile Component manually.");
                }
                
                p.SetActive(true);
                
                Vector3 flyDir = transform.forward;
                Vector3 localFlyDir = p.transform.InverseTransformDirection(flyDir);
                
                Debug.Log($"[Enemy {name}] Launching Projectile. Dir: {flyDir}, Speed: 15");
                proj.Launch(flyDir, 15f, 1, false, moveAxis: localFlyDir); 
            }
            else
            {
                 Debug.LogError($"[Enemy {name}] Failed to create projectile object!");
            }

            yield return new WaitForSeconds(0.2f);
        }
    }
}
