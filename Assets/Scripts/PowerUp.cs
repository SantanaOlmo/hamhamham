using UnityEngine;

public enum PowerUpType { Health, Speed, TimeStop, Shield, Bomb, Ammo, Turret }

public class PowerUp : MonoBehaviour
{
    private PowerUpType type;
    private GameObject visual;
    private float startY;
    private float floatSpeed = 2.0f;
    private float floatAmplitude = 0.5f;

    public void Setup(GameObject prefab, float amplitude, PowerUpType type)
    {
        this.type = type;
        this.floatAmplitude = amplitude;
        
        // Remove existing visuals if any (re-pooling support)
        if (visual != null) Destroy(visual);

        if (prefab != null)
        {
            visual = Instantiate(prefab, transform);
            visual.transform.localPosition = Vector3.zero;
            
            // SAFETY: Strip logic components if an agent prefab was accidentally assigned
            // This prevents "Ghost Turrets" or "Ghost Enemies" from floating around
            if (visual.GetComponent<Turret>() != null) Destroy(visual.GetComponent<Turret>());
            if (visual.GetComponent<Rigidbody>() != null) Destroy(visual.GetComponent<Rigidbody>());
            
            // Remove all colliders from visual to prevent physics conflicts with the PowerUp info
            foreach(var col in visual.GetComponentsInChildren<Collider>()) Destroy(col);
            
            // Removed rotation/scale override to trust the prefab's settings
            // visual.transform.localRotation = Quaternion.identity; 
        }
        else
        {
            // Default Cube Fallback
            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(transform, false);
            Destroy(visual.GetComponent<Collider>());
            Renderer r = visual.GetComponent<Renderer>();
            
            Color c = Color.white;
            switch(type)
            {
                case PowerUpType.Health: c = Color.red; break;
                case PowerUpType.Speed: c = Color.blue; break;
                case PowerUpType.Shield: c = Color.cyan; break;
                case PowerUpType.TimeStop: c = Color.yellow; break;
                case PowerUpType.Bomb: c = new Color(1f, 0.5f, 0f); break; // Orange
                case PowerUpType.Ammo: c = Color.green; break;
                case PowerUpType.Turret: c = Color.magenta; break; // Magenta for Turret
            }
            r.material.color = c;
            visual.transform.localScale = Vector3.one * 0.8f;
        }
        
        // Physics Trigger
        if (GetComponent<SphereCollider>() == null)
        {
            SphereCollider col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;
        }

        startY = transform.position.y;
    }

    void Start() 
    {
        // Debug
        // Debug.Log($"PowerUp initialized at {transform.position}, Scale: {transform.localScale}, Visual: {(visual != null ? visual.name : "NULL")}");
    }

    void Update()
    {
        // Check for Time Stop
        if (GameManager.Instance != null && GameManager.Instance.IsTimeFrozen) return;

        // Float logic
        float newY = startY + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Simple Rotation (Spin)
        if (visual != null)
        {
             visual.transform.Rotate(Vector3.up, 50 * Time.deltaTime, Space.World);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            // Play Pickup SFX
            if (SoundManager.Instance != null)
            {
                 AudioClip clip = null;
                 switch(type)
                 {
                     case PowerUpType.Health: clip = SoundManager.Instance.sfxHealth; break;
                     case PowerUpType.Speed: clip = SoundManager.Instance.sfxSpeed; break;
                     case PowerUpType.Shield: clip = SoundManager.Instance.sfxShield; break;
                     case PowerUpType.TimeStop: clip = SoundManager.Instance.sfxTimeStop; break;
                     case PowerUpType.Bomb: clip = SoundManager.Instance.sfxBomb; break; // Use Bomb SFX
                     case PowerUpType.Ammo: clip = SoundManager.Instance.sfxAmmo; break;
                     case PowerUpType.Turret: clip = SoundManager.Instance.sfxTurret; break; // Use Turret SFX
                 }
                 if (clip != null) SoundManager.Instance.PlaySFX(clip);
            }

            switch (type)
            {
                case PowerUpType.Health:
                    if (player.CurrentHealth < 3) player.HealFull();
                    else player.Heal(1);
                    break;
                case PowerUpType.Speed:
                    float amount = (GameManager.Instance != null) ? GameManager.Instance.speedBoostAmount : 1.5f;
                    player.IncreaseSpeed(amount); 
                    break;
                case PowerUpType.TimeStop:
                    // Store instead of freeze
                    if (GameManager.Instance != null) GameManager.Instance.StoreTimeStop();
                    break;
                case PowerUpType.Shield:
                    player.ActivateShield(5); // 5 hits
                    break;
                case PowerUpType.Bomb:
                    if (GameManager.Instance != null) GameManager.Instance.EnableSpecialAbility();
                    break;
                case PowerUpType.Ammo:
                    player.ActivateAmmoPowerUp();
                    break;
                case PowerUpType.Turret:
                    if (GameManager.Instance != null) GameManager.Instance.StoreTurret();
                    break;
            }
            Destroy(gameObject);
        }
    }
}
