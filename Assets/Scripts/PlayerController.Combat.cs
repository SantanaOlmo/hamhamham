using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public partial class PlayerController : MonoBehaviour
{
    // Health
    public int maxHealth = 3;
    public int CurrentHealth { get; set; } = 3;
    
    // Invulnerability
    public bool IsInvulnerable { get; private set; } = false;
    private float invulnerabilityDuration = 2.0f;
    
    // Abilities
    public int ShieldCharges { get; private set; } = 0;
    private GameObject shieldVisual;
    private bool isShieldBlinking = false;

    // Shooting
    private float bpm = 128f;
    private float fireRate; 
    private float lastFireTime;
    public GameObject projectilePrefab; 

    // AMMO POWERUP
    public bool HasAmmoPowerUp { get; private set; } = false;

    void HandleShooting()
    {
        bool firePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
        
        if (firePressed && Time.time > lastFireTime + fireRate)
        {
            lastFireTime = Time.time;
            Shoot();
        }
    }

    void Shoot()
    {
        if (SoundManager.Instance != null && SoundManager.Instance.sfxShoot != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.sfxShoot);
        }

        GameObject projGo = null;
        
        // 1. Determine Prefab and Rotation
        GameObject prefabToUse = projectilePrefab; // Default local
        Vector3 rotOffset = Vector3.zero;

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.playerProjectilePrefab != null) 
            {
                prefabToUse = GameManager.Instance.playerProjectilePrefab;
            }
            rotOffset = GameManager.Instance.playerProjectileRotationOffset;
        }

        // 2. Instantiate (Pool or New)
        // We apply the rotation offset AT SPAWN so the visuals are correct immediately.
        Quaternion spawnRotation = transform.rotation * Quaternion.Euler(rotOffset);
        Vector3 spawnPos = transform.position + transform.forward * 0.7f;

        if (ObjectPoolManager.Instance != null && prefabToUse != null)
        {
            projGo = ObjectPoolManager.Instance.SpawnFromPool(prefabToUse, spawnPos, spawnRotation);
        }

        if (projGo == null)
        {
             if (prefabToUse != null)
             {
                 projGo = Instantiate(prefabToUse, spawnPos, spawnRotation);
             }
             else
             {
                 projGo = new GameObject("Projectile");
                 projGo.transform.position = spawnPos; 
                 projGo.transform.rotation = spawnRotation;
                 projGo.AddComponent<Projectile>();
             }
        }
        
        // 3. Launch
        if (projGo != null)
        {
            Projectile p = projGo.GetComponent<Projectile>();
             if (p == null)
             {
                 p = projGo.AddComponent<Projectile>();
             }
             
             if (p != null)
             {
                 float shotSpeed = 20f + moveSpeed;
                 
                 // MOVEMENT DIRECTION CALCULATION
                 // The visual object 'p' is rotated by 'rotOffset'.
                 // But we want it to fly in the Player's forward direction ('transform.forward').
                 // We need to transform that global direction into 'p's' local space.
                 Vector3 flyDir = transform.forward;
                 Vector3 localAxis = projGo.transform.InverseTransformDirection(flyDir);
                 
                 p.Launch(flyDir, shotSpeed, 1, true, moveAxis: localAxis);
             }
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsInvulnerable) return;

        if (ShieldCharges > 0)
        {
            ShieldCharges--;
            StartCoroutine(ShieldBlinkRoutine());
            UpdateShieldVisuals();
            return; 
        }

        CurrentHealth -= amount;
        
        if (SoundManager.Instance != null)
        {
             if (CurrentHealth > 0 && SoundManager.Instance.sfxPlayerDamage != null)
                 SoundManager.Instance.PlaySFX(SoundManager.Instance.sfxPlayerDamage);
             else if (CurrentHealth <= 0 && SoundManager.Instance.sfxPlayerDeath != null)
                 SoundManager.Instance.PlaySFX(SoundManager.Instance.sfxPlayerDeath);
        }

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            GameManager.Instance.GameOver();
            Destroy(gameObject);
        }
        else
        {
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    IEnumerator InvulnerabilityRoutine()
    {
        IsInvulnerable = true;
        float elapsed = 0f;
        while (elapsed < invulnerabilityDuration)
        {
            if (rend != null) rend.enabled = !rend.enabled; 
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        if (rend != null) rend.enabled = true;
        IsInvulnerable = false;
    }

    public void HealFull()
    {
        CurrentHealth = 3;
    }

    public void Heal(int amount)
    {
        CurrentHealth += amount;
        if (CurrentHealth > 20) CurrentHealth = 20; 
    }

    public void ActivateShield(int charges)
    {
        ShieldCharges += charges; 
        if (ShieldCharges > 5) ShieldCharges = 5;
        UpdateShieldVisuals();
    }
    
    void UpdateShieldVisuals()
    {
        bool shouldBeActive = (ShieldCharges > 0);
        
        if (!shouldBeActive)
        {
             if (shieldVisual != null && !isShieldBlinking) shieldVisual.SetActive(false);
             return;
        }

        if (shieldVisual == null)
        {
            shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldVisual.name = "ShieldVisual";
            shieldVisual.transform.SetParent(this.transform, false);
            Destroy(shieldVisual.GetComponent<Collider>()); 
            
            float sScale = (GameManager.Instance != null) ? GameManager.Instance.shieldScale : 4.5f;
            shieldVisual.transform.localScale = Vector3.one * sScale;
            
            Renderer r = shieldVisual.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Sprites/Default"));
        }
        
        shieldVisual.SetActive(true);
        float sScaleParam = (GameManager.Instance != null) ? GameManager.Instance.shieldScale : 4.5f;
        shieldVisual.transform.localScale = Vector3.one * sScaleParam;
        
        float opacity = Mathf.Lerp(0.2f, 0.5f, (ShieldCharges - 1) / 4f);
        if (ShieldCharges == 0) opacity = 0; 
        
        Renderer rend = shieldVisual.GetComponent<Renderer>();
        if (rend != null)
        {
             Color c = Color.cyan; 
             c.a = opacity;
             rend.material.color = c;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (ShieldCharges > 0)
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                // enemy.Die(); // USER REQUEST: Do not kill enemy on collision
                ShieldCharges--;
                StartCoroutine(ShieldBlinkRoutine());
                UpdateShieldVisuals();
            }
        }
    }
    
    IEnumerator ShieldBlinkRoutine()
    {
        if (shieldVisual == null) yield break;
        isShieldBlinking = true;
        
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            shieldVisual.SetActive(!shieldVisual.activeSelf);
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        isShieldBlinking = false;
        UpdateShieldVisuals();
    }

    public void ActivateAmmoPowerUp()
    {
        HasAmmoPowerUp = true;
        fireRate /= 2f; 
        if (fireRate < 0.05f) fireRate = 0.05f; 
        // Debug.Log($"AMMO POWERUP ACTIVATED: New Rate {fireRate}");
    }
}
