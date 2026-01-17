using UnityEngine;

public class CrowdMember : MonoBehaviour
{
    private float hopSpeed;
    private float hopAmplitude = 0.5f;
    private float timeOffset;
    private Vector3 initialPos;

    public void Setup(Color bodyColor, float jumpSpeed, float amplitude, float offset, int lodLevel, Material baseMat = null)
    {
        this.hopSpeed = jumpSpeed;
        this.hopAmplitude = amplitude;
        this.timeOffset = offset;

        // Visual Generation
        // Body (Cylinder)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.transform.SetParent(transform, false);
        body.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f); 
        body.transform.localPosition = new Vector3(0, 0.8f, 0);
        Destroy(body.GetComponent<Collider>());
        
        Renderer bodyRend = body.GetComponent<Renderer>();
        if (bodyRend != null)
        {
             // Apply Base Material first (defines shader/properties)
             if (baseMat != null) bodyRend.material = baseMat;
             
             // Then tint it
             bodyRend.material.color = bodyColor;
        }

        // Head (Sphere)
        if (lodLevel < 5) 
        {
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(transform, false);
            head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            head.transform.localPosition = new Vector3(0, 1.8f, 0);
            Destroy(head.GetComponent<Collider>());
            
            Renderer headRend = head.GetComponent<Renderer>();
            Color skinColor = new Color(1f, 0.8f, 0.6f);
            if (headRend != null)
            {
                 if (baseMat != null) headRend.material = baseMat;
                 
                 // Fade head too
                 headRend.material.color = Color.Lerp(skinColor, Color.black, lodLevel * 0.1f);
            }
        }
    }

    void Start()
    {
        initialPos = transform.localPosition;
    }

    public void SetSpeed(float newSpeed)
    {
        this.hopSpeed = newSpeed;
    }

    void Update()
    {
        // Simple Bounce
        float yOffset = Mathf.Abs(Mathf.Sin((Time.time * hopSpeed) + timeOffset)) * hopAmplitude;
        transform.localPosition = initialPos + new Vector3(0, yOffset, 0);
    }
}
