using UnityEngine;

public class CrowdGenerator : MonoBehaviour
{
    public int rows = 10;
    public int peoplePerRow = 50; // Will be overriden by GM or width calculations
    public float spacing = 2.0f;
    public float rowHeightDiff = 1.0f;
    public float rowDepthSpacing = 1.5f;
    public float jumpSpeed = 3.0f;
    
    public Material crowdMaterial; // Assigned by GM

    // Config from GM
    public void GenerateBleachers(Vector3 center, Vector3 forwardDir, float width)
    {
        // ... (lines 15-30 skipped, same logic)
        // Spawn Root
        GameObject root = new GameObject("BleacherRoot");
        root.transform.SetParent(transform, false);
        root.transform.position = center;
        root.transform.rotation = Quaternion.LookRotation(forwardDir); 
        
        float startX = -(width / 2f);
        
        for (int r = 0; r < rows; r++)
        {
            // ... (lines 35-45 same)
            Vector3 rowPos = new Vector3(0, r * rowHeightDiff, -r * rowDepthSpacing);
            float darken = (float)r / rows;
            Color bodyColor = Random.Range(0,2) == 0 ? Color.red : Color.blue;
            bodyColor = Color.Lerp(bodyColor, Color.black, darken * 0.9f); 
            
            for (int c = 0; c < peoplePerRow; c++)
            {
                // ...
                float x = startX + (c * spacing);
                
                GameObject memberGo = new GameObject($"Member_{r}_{c}");
                memberGo.transform.SetParent(root.transform, false);
                memberGo.transform.localPosition = rowPos + new Vector3(x, 0, 0);
                
                CrowdMember member = memberGo.AddComponent<CrowdMember>();
                float speed = jumpSpeed + Random.Range(-0.5f, 0.5f);
                float offset = Random.Range(0f, 10f);
                float amp = 0.5f + Random.Range(0f, 0.2f);
                
                // Pass material here
                member.Setup(bodyColor, speed, amp, offset, r, crowdMaterial);
            }
        }
    }
    public void SetCrowdSpeed(float newSpeed)
    {
        this.jumpSpeed = newSpeed;
        CrowdMember[] members = GetComponentsInChildren<CrowdMember>();
        foreach(var m in members)
        {
            if(m != null) m.SetSpeed(newSpeed);
        }
    }
}
