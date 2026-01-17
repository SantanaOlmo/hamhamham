using UnityEngine;

public class GhostObject : MonoBehaviour
{
    private float duration;
    private float elapsed;
    private Renderer rend;
    private Material matInstance;

    private void SetupMaterialForTransparency(Material mat)
    {
        // URP Lit Shader properties
        // _Surface: 0 = Opaque, 1 = Transparent
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1.0f);
        
        // Use standard blend mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        
        // Keywords for URP
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        
        // Also handling Standard shader keywords as backup
        mat.EnableKeyword("_ALPHABLEND_ON");
        
        mat.renderQueue = 3000;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float progress = elapsed / duration;
        
        if (progress >= 1f)
        {
            // Return to pool using the name as key (GhostTrail sets this name)
            ObjectPoolManager.Instance.ReturnToPool(gameObject, gameObject.name); 
        }
        else
        {
            UpdateAlpha(progress);
        }
    }

    private float originalAlpha = 1f;

    public void Init(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material originalMat, float durationTime)
    {
        this.duration = durationTime;
        this.elapsed = 0f;

        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = scale;

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        rend = GetComponent<Renderer>();
        if (rend == null) rend = gameObject.AddComponent<MeshRenderer>();
        
        // DISABLE SHADOWS:
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        // OPTIMIZATION: Recycle Material Instance
        // Only create a new one if we don't have one, or if for some reason we want to support changing source materials dynamically.
        // Assuming player material doesn't change often, we keep the instance.
        
        if (matInstance == null)
        {
            matInstance = new Material(originalMat);
            SetupMaterialForTransparency(matInstance);
        }
        else
        {
            // Reset material properties if needed (e.g. if originalMat changed properties we care about)
            // For now, valid to just reuse the existing transparent clone.
            // Ensure alpha is reset to start value in Update loop or here.
        }

        rend.material = matInstance;
        
        // Reset Alpha tracking
        // Capture original alpha from the *Original* material? Or the instance?
        // Reuse the instance's base properties but ensure we start fresh.
        if (originalMat.HasProperty("_BaseColor")) originalAlpha = originalMat.GetColor("_BaseColor").a;
        else if (originalMat.HasProperty("_Color")) originalAlpha = originalMat.color.a;
        else originalAlpha = 1f;
        
        // REDUCE OPACITY: User requested "50% more transparent" and "much lower opacity".
        // Multiplying by 0.3f to make it very subtle.
        originalAlpha *= 0.3f;
        
        // Force initial update
        UpdateAlpha(0f);
    }
    
    void UpdateAlpha(float progress)
    {
        float alpha = Mathf.Lerp(originalAlpha, 0f, progress); 
            
        if (rend.material.HasProperty("_BaseColor"))
        {
            Color c = rend.material.GetColor("_BaseColor");
            c.a = alpha;
            rend.material.SetColor("_BaseColor", c);
        }
        
        if (rend.material.HasProperty("_Color"))
        {
            Color c = rend.material.color;
            c.a = alpha;
            rend.material.color = c;
        }
    }
}
