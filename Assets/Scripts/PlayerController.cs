using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerController : MonoBehaviour
{
    public float moveSpeed = 8.0f;
    public float dashSpeed = 50.0f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1.0f;

    private Camera mainCamera;
    private Renderer rend; 

    void Start()
    {
        // Calculate Fire Rate: 8th notes
        fireRate = (60f / bpm) / 2f; 

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true; 

        rend = GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(this.transform, false);
            visual.name = "PlayerVisual";
            Destroy(visual.GetComponent<Collider>());
            rend = visual.GetComponent<Renderer>();
            if (rend != null) rend.material.color = Color.white;
        }

        // RB is added by GameManager now to support custom configs
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
             // Fallback if not initialized by GameManager
             rb = gameObject.AddComponent<Rigidbody>();
             rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
             rb.mass = 1f;
             rb.linearDamping = 5f; 
        }

        // Collider is also handled by GameManager (Configurable Shape/Size)
        // We do *not* add a default BoxCollider here anymore. 

        mainCamera = Camera.main;
        
        gameObject.tag = "Player";
        
        // Laser Sight Init
        laserLine = GetComponent<LineRenderer>();
        if (laserLine == null) laserLine = gameObject.AddComponent<LineRenderer>();
        laserLine.startWidth = 0.1f;
        laserLine.endWidth = 0.1f;
        laserLine.material = new Material(Shader.Find("Sprites/Default"));
        laserLine.positionCount = 2;
        laserLine.enabled = false;
    }
    
    void Awake()
    {
        ghostTrail = GetComponent<GhostTrail>();
        if (ghostTrail == null) ghostTrail = gameObject.AddComponent<GhostTrail>();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.PLAYING) return;
        if (GameManager.Instance.IsIntroSequence) return; 

        HandleRotation();
        HandleShooting();
        HandleDash();
        UpdateLaser();
    }

    void UpdateLaser()
    {
        if (laserLine == null) return;
        
        GameManager gm = GameManager.Instance;
        if (!gm.showLaserSight)
        {
            laserLine.enabled = false;
            return;
        }

        laserLine.enabled = true;
        
        // MATERIAL ASSIGNMENT
        if (gm.laserMaterial != null)
        {
            if (laserLine.sharedMaterial != gm.laserMaterial) 
                laserLine.sharedMaterial = gm.laserMaterial;
                
            // Reset colors to White so the Material controls the visual
            laserLine.startColor = Color.white;
            laserLine.endColor = Color.white;
        }
        else
        {
            // Fallback if no material assigned but enabled
             laserLine.startColor = Color.red;
             laserLine.endColor = new Color(1, 0, 0, 0);
        }
        
        Vector3 startPos = transform.position + new Vector3(0, 0.5f, 0); // Slight height offset
        Vector3 endPos = startPos + transform.forward * 50f; // Long range

        laserLine.SetPosition(0, startPos);
        laserLine.SetPosition(1, endPos);
    }
    
    // Laser Ref
    private LineRenderer laserLine;

    void FixedUpdate()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.PLAYING) return;
        if (GameManager.Instance.IsIntroSequence) return; 
        
        if (!isDashing)
        {
            HandleMovement();
        }
    }

    void LateUpdate()
    {
        if (GameManager.Instance == null) return;
        ClampPosition();
    }
}

