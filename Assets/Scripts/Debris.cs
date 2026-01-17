using UnityEngine;

public class Debris : MonoBehaviour
{
    private Renderer rend;
    private float lifetime = 1.0f; // Time before fade starts
    private float fadeDuration = 1.0f; 

    void Start()
    {
        // 1. LAYERS & COLLISION: 
        // Set Layer to "Ignore Raycast" (2) so we can filter collisions via Physics Matrix
        gameObject.layer = 2; 
        
        // We KEEP the Collider now, because we want it to hit the floor.
        // The Collision Matrix in GameManager will prevent it hitting the Player (Layer 0).

        // 2. PHYSICS: Add explosive force
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        
        rb.mass = 0.1f; // Very light
        rb.linearDamping = 0.5f;
        
        // PHYSICS: Configurable Force
        float explForce = (GameManager.Instance != null) ? GameManager.Instance.debrisExplosionForce : 5f;
        float upForce = (GameManager.Instance != null) ? GameManager.Instance.debrisUpwardForce : 0.5f;

        // "Salir disparados levemente hacia los lados"
        // Flatten the random direction to be mostly horizontal
        Vector3 randomDir = Random.insideUnitSphere;
        randomDir.y = upForce; // Configurable upward component
        randomDir.Normalize();
        
        rb.AddForce(randomDir * explForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 5f);

        // 3. VISUALS
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Ensure Material allows Transparency/Fade
            // Attempt to force Standard Shader Fade mode or similar
            // This is "hacky" but necessary if we don't control the source material asset settings
            Material m = rend.material; // Creates instance
            if (m.shader.name == "Standard" || m.shader.name == "Universal Render Pipeline/Lit")
            {
                 // Start Fade Mode setup for Standard Shader
                 m.SetFloat("_Mode", 2); // 2 = Fade
                 m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                 m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                 m.SetInt("_ZWrite", 0);
                 m.DisableKeyword("_ALPHATEST_ON");
                 m.EnableKeyword("_ALPHABLEND_ON");
                 m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                 m.renderQueue = 3000;
            }
            else if (m.shader.name == "Sprites/Default")
            {
                // Already supports alpha
            }
        }
        
        StartCoroutine(FadeRoutine());
    }

    System.Collections.IEnumerator FadeRoutine()
    {
        // Wait for the "Stay" period
        yield return new WaitForSeconds(lifetime);
        
        float timer = 0f;
        Color startColor = (rend != null) ? rend.material.color : Color.white;
        
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;
            
            // Linear Fade
            float alpha = Mathf.Lerp(startColor.a, 0f, progress);
            
            if (rend != null)
            {
                 Color c = startColor;
                 c.a = alpha;
                 rend.material.color = c;
            }
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
}
