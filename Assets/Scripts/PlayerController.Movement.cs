using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public partial class PlayerController : MonoBehaviour
{
    // Physics
    private Rigidbody rb;
    private float lastDashTime = -10f;
    private bool isDashing = false;

    // Ghost Trail Reference
    private GhostTrail ghostTrail;
    
    // Stats Tracking
    public int SpeedUpgradeCount { get; private set; } = 0;

    void HandleMovement()
    {
        float h = 0f;
        float v = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h = 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v = -1f;
        }

        Vector3 dir = new Vector3(h, 0, v).normalized;
        if (dir.magnitude > 0.1f)
        {
            rb.linearVelocity = new Vector3(dir.x * moveSpeed, rb.linearVelocity.y, dir.z * moveSpeed);
        }
        else
        {
             rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void ClampPosition()
    {
        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, -48f, 48f);
        clampedPos.z = Mathf.Clamp(clampedPos.z, -48f, 48f);
        transform.position = clampedPos;
    }

    void HandleDash()
    {
        bool dashPressed = Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame);
        
        if (dashPressed && Time.time > lastDashTime + dashCooldown)
        {
            StartCoroutine(DashRoutine());
        }
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;
        lastDashTime = Time.time;
        
        // GHOST MODE: Switch to Layer 2 (Ignore Raycast) to ignore Layer 0 (Enemies/Walls)
        // Ensure Floor is Layer 4 (Water) so we don't fall.
        int oldLayer = gameObject.layer;
        gameObject.layer = 2; 
        
        if (ghostTrail != null) ghostTrail.StartTrail();

        float h = 0f;
        float v = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h = 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v = -1f;
        }
        Vector3 dashDir = new Vector3(h, 0, v).normalized;
        
        if (dashDir.magnitude < 0.1f) dashDir = transform.forward;

        float currentDashSpeed = Mathf.Max(dashSpeed, moveSpeed * 3.0f);

        rb.linearVelocity = dashDir * currentDashSpeed;
        
        yield return new WaitForSeconds(dashDuration);
        
        isDashing = false;
        rb.linearVelocity = Vector3.zero; 
        gameObject.layer = oldLayer; // Restore Collision
        
        yield return new WaitForSeconds(0.25f);
        
        if (ghostTrail != null) ghostTrail.StopTrail();
    }
    
    void HandleRotation()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsIntroSequence) return;
        if (Mouse.current == null) return;
        if (mainCamera == null) return; // Defensive check
        
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        
        // ALIGNMENT FIX:
        // Raycast against a plane at the same height as the Laser/Weapon (approx Player Y + 0.5f)
        // This prevents parallax issues where the cursor and laser diverge on screen.
        float aimHeight = transform.position.y + 0.5f; 
        Plane aimPlane = new Plane(Vector3.up, new Vector3(0, aimHeight, 0));
        
        float rayDistance;

        if (aimPlane.Raycast(ray, out rayDistance))
        {
            Vector3 point = ray.GetPoint(rayDistance);
            Vector3 lookDir = point - transform.position;
            lookDir.y = 0; // Keep looking horizontal
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }
    
    public void IncreaseSpeed(float amount)
    {
        moveSpeed += amount;
        SpeedUpgradeCount++;
    }
}
