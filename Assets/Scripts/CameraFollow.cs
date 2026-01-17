using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    private bool isIntro = false;
    private Vector3 finalOffset;
    private Quaternion finalRotation;

    void Start()
    {
        // If Intro has already started (triggered by GM), don't overwrite the correct offset
        // with the animated (low) position.
        if (isIntro) return;

        if (target != null)
        {
            // Only capture offset if it hasn't been explicitly set (e.g. by GameManager)
            if (offset == Vector3.zero)
            {
                offset = transform.position - target.position;
            }
            finalOffset = offset;
            finalRotation = transform.rotation;
        }
    }

    public void StartIntro(float duration)
    {
        if (target == null) return;
        
        // If offset is already set (e.g. by GameManager), use it.
        // Otherwise, capture from current transform.
        if (offset == Vector3.zero)
        {
             offset = transform.position - target.position;
        }
        
        finalOffset = offset;
        
        // Ensure final rotation is looking at the target (standard top-down behavior)
        // If GameManager snapped it, transform.rotation is already correct. 
        // We capture it here.
        finalRotation = transform.rotation;
        
        StartCoroutine(IntroRoutine(duration));
    }

    System.Collections.IEnumerator IntroRoutine(float duration)
    {
        isIntro = true;
        
        // Decompose Final Offset (typically 0, 20, -20)
        float finalDist = new Vector3(finalOffset.x, 0, finalOffset.z).magnitude; // Horizontal Dist
        float finalHeight = finalOffset.y;
        float finalAngle = Mathf.Atan2(finalOffset.x, finalOffset.z) * Mathf.Rad2Deg; // typically 180 or 0 depending on setup
        
        // Start Params (Front, Low, Close)
        // Opposing side: Rotate 180 from final? 
        // If final is Back (Z = -20), Angle is 180? (Atan2(0, -20) = 180).
        // We want Start at Front (Z = +10). Angle 0.
        // Let's force a nice sweep. 
        float startAngle = finalAngle + 180f; 
        float startHeight = 2.0f;
        float startDist = 8.0f; // Close up
        
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            // Smooth ease in/out
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Interpolate Cylindrical Coords
            float currentAngle = Mathf.Lerp(startAngle, finalAngle, smoothT);
            float currentHeight = Mathf.Lerp(startHeight, finalHeight, smoothT);
            float currentDist = Mathf.Lerp(startDist, finalDist, smoothT);
            
            // Reconstruct Position relative to Target
            // Angle to Direction
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            Vector3 dir = rotation * Vector3.forward; // Z forward rotated
            
            Vector3 relativePos = dir * currentDist;
            relativePos.y = currentHeight;
            
            // Apply Position
            Vector3 nextPos = target.position + relativePos;
            transform.position = nextPos;
            
            // SMOOTH LANDING ROTATION:
            // 1. Calculate ideal tracking rotation (Look at Head)
            Quaternion lookRot = Quaternion.LookRotation(target.position + Vector3.up * 1.5f - nextPos);
            
            // 2. Blend towards final fixed rotation as we near the end
            // Use smoothT^2 to keep focus on player longer, then ease into fixed angle.
            transform.rotation = Quaternion.Slerp(lookRot, finalRotation, smoothT * smoothT);

            yield return null;
        }

        // Snap to final
        transform.position = target.position + finalOffset;
        transform.rotation = finalRotation;
        isIntro = false;
    }

    void LateUpdate()
    {
        if (target == null || isIntro) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
