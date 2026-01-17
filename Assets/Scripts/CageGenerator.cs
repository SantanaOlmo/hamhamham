using UnityEngine;

public class CageGenerator : MonoBehaviour
{
    public float areaSize = 100f; // Total size of the square area (e.g. 100x100)
    public float height = 10f;
    public float verticalSpacing = 5f;
    public float horizontalSpacing = 2f;
    public float barThickness = 0.5f;

    public Texture2D[] crowdFrames; // Injected from GameManager
    public Material cageMaterial; // Injected from GameManager

    public void GenerateCage()
    {
        // Clear previous cage if any
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        float halfSize = areaSize / 2f;

        // Create 4 Walls
        // Wall 1: +Z (Back)
        CreateWall(new Vector3(0, height/2f, halfSize), new Vector3(areaSize, height, 0), false);
        
        // Wall 2: -Z (Front)
        CreateWall(new Vector3(0, height/2f, -halfSize), new Vector3(areaSize, height, 0), false);
        
        // Wall 3: +X (Right)
        CreateWall(new Vector3(halfSize, height/2f, 0), new Vector3(0, height, areaSize), true);
        
        // Wall 4: -X (Left)
        CreateWall(new Vector3(-halfSize, height/2f, 0), new Vector3(0, height, areaSize), true);
    }

    void CreateWall(Vector3 center, Vector3 size, bool isSideways)
    {
        GameObject wallRoot = new GameObject("Wall");
        wallRoot.transform.SetParent(transform, false);
        
        float width = isSideways ? size.z : size.x;
        
        // Vertical Bars
        int vCount = Mathf.FloorToInt(width / verticalSpacing);
        for (int i = 0; i <= vCount; i++)
        {
            float t = (float)i / vCount;
            float posOffset = Mathf.Lerp(-width/2, width/2, t);
            
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bar.name = "V_Bar";
            bar.transform.SetParent(wallRoot.transform, false);
            
            // Remove collider if we don't want physics, or keep it for containment
            // BoxCollider is better for walls, but cylinder works for visuals.
            // Let's keep colliders so it acts as a boundary.
            
            if (isSideways)
                bar.transform.position = center + new Vector3(0, 0, posOffset);
            else
                bar.transform.position = center + new Vector3(posOffset, 0, 0);

            bar.transform.localScale = new Vector3(barThickness, height / 2f, barThickness); 
            
            // Visuals
            Renderer r = bar.GetComponent<Renderer>();
            if(r != null)
            {
                 if (cageMaterial != null) 
                     r.material = cageMaterial;
                 else 
                     r.material.color = Color.gray;
            }
        }

        // Horizontal Bars
        int hCount = Mathf.FloorToInt(height / horizontalSpacing);
        for (int i = 0; i < hCount; i++)
        {
            float yPos = (i + 1) * horizontalSpacing;
            if (yPos >= height) break;

            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bar.name = "H_Bar";
            bar.transform.SetParent(wallRoot.transform, false);

            if (isSideways)
            {
                bar.transform.position = center + new Vector3(0, yPos - height/2f, 0); // Adjust for center offset
                bar.transform.rotation = Quaternion.Euler(90, 0, 0); // Rotate to lie along Z? No, Cylinder is Y up.
                                                                     // Sideways wall needs bars along Z.
                                                                     // Create Cylinder along Y (Default). Rotate 90 X -> Along Z.
                bar.transform.localScale = new Vector3(barThickness, width / 2f, barThickness);
            }
            else
            {
                bar.transform.position = center + new Vector3(0, yPos - height/2f, 0);
                bar.transform.rotation = Quaternion.Euler(0, 0, 90); // Rotate to lie along X.
                bar.transform.localScale = new Vector3(barThickness, width / 2f, barThickness);
            }
            
            // Visuals
            Renderer r = bar.GetComponent<Renderer>();
            if(r != null)
            {
                 if (cageMaterial != null) 
                     r.material = cageMaterial;
                 else 
                     r.material.color = Color.gray;
            }
        }
    }
}
