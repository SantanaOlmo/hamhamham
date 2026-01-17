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
    }

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

