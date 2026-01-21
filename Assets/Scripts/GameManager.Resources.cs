using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public partial class GameManager : MonoBehaviour
{
    // High Score Data Structures
    [System.Serializable]
    public class HighScoreEntry
    {
        public string name;
        public int score;
    }
    [System.Serializable]
    public class HighScoreTable
    {
        public List<HighScoreEntry> entries = new List<HighScoreEntry>();
    }
    public HighScoreTable highScoreTable = new HighScoreTable();
    public string playerName = "Pilot";

    IEnumerator LoadResources()
    {
        string titlePath = System.IO.Path.Combine(Application.dataPath, "titulo.png");
        if (System.IO.File.Exists(titlePath))
        {
            byte[] fileData = System.IO.File.ReadAllBytes(titlePath);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(fileData))
            {
                if(uiManager) uiManager.titleTexture = tex;
            }
        }
        yield return null;
    }

    void LoadHighScores()
    {
        string json = PlayerPrefs.GetString("highScoreTable", "");
        if (!string.IsNullOrEmpty(json))
        {
            highScoreTable = JsonUtility.FromJson<HighScoreTable>(json);
        }
        
        if(highScoreTable == null) highScoreTable = new HighScoreTable();
        highScoreTable.entries.Sort((a,b) => b.score.CompareTo(a.score));
    }

    void SubmitScore(int score)
    {
        if (scoreSubmitted) return;
        scoreSubmitted = true;

        HighScoreEntry entry = new HighScoreEntry { name = playerName, score = score };
        highScoreTable.entries.Add(entry);
        highScoreTable.entries.Sort((a,b) => b.score.CompareTo(a.score));
        
        if(highScoreTable.entries.Count > 5) highScoreTable.entries.RemoveRange(5, highScoreTable.entries.Count - 5);
        
        string json = JsonUtility.ToJson(highScoreTable);
        PlayerPrefs.SetString("highScoreTable", json);
        PlayerPrefs.Save();
    }

    void StopMusic()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.StopMusic();
    }

    void CleanupScene()
    {
        var debris = FindObjectsByType<Debris>(FindObjectsSortMode.None);
        foreach(var d in debris) Destroy(d.gameObject);
        
        var projectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        foreach(var p in projectiles) Destroy(p.gameObject);
        
        var powerups = FindObjectsByType<PowerUp>(FindObjectsSortMode.None);
        foreach(var pu in powerups) Destroy(pu.gameObject);
        
        var turrets = FindObjectsByType<Turret>(FindObjectsSortMode.None);
        foreach(var t in turrets) Destroy(t.gameObject);
        
        // Don't destroy environment every time if it persists, but rebuilding is safer
        // Destroy(GameObject.Find("Environment")); 
        
        if (waveSpawner != null) waveSpawner.ClearDeferredDrops();
    }
    
    void BootstrapScene()
    {
        // 1. Create Environment (Floor, Cage, Crowd)
        CreateEnvironment();
        CreateCage();
        CreateCrowd();

        // 2. Create Player Root (Logic/Physics)
        GameObject playerRoot = new GameObject("Player");
        playerRoot.transform.position = new Vector3(0, 1f, 0); 
        playerRoot.transform.localScale = Vector3.one * playerScale; // Apply Scale
        
        // Instantiate Visuals as Child
        if (playerPrefab != null)
        {
            GameObject visual = Instantiate(playerPrefab, playerRoot.transform);
            visual.transform.localPosition = new Vector3(0, playerVisualHeightOffset, 0); // Apply Height Offset
            visual.transform.localRotation = Quaternion.Euler(playerRotationOffset);
        }
        else
        {
             GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
             visual.transform.SetParent(playerRoot.transform, false);
        }
        
        // COLLIDER & PHYSICS GENERATION
        // RigidBody (Must be on Root)
        Rigidbody rb = playerRoot.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.mass = 1f;
        rb.linearDamping = 5f;

        // Custom Collider
        if (playerColliderType == ColliderType.Box)
        {
            BoxCollider col = playerRoot.AddComponent<BoxCollider>();
            col.center = playerColliderOffset;
            col.size = playerColliderSize;
        }
        else
        {
            SphereCollider col = playerRoot.AddComponent<SphereCollider>();
            col.center = playerColliderOffset;
            col.radius = Mathf.Max(playerColliderSize.x, playerColliderSize.y, playerColliderSize.z) / 2f;
        }

        // Debug Visual for Collider
        if (showPlayerCollider)
        {
            GameObject debugCol = null;
            if (playerColliderType == ColliderType.Box)
            {
                debugCol = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debugCol.transform.SetParent(playerRoot.transform, false);
                debugCol.transform.localPosition = playerColliderOffset;
                debugCol.transform.localScale = playerColliderSize;
            }
            else
            {
                debugCol = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugCol.transform.SetParent(playerRoot.transform, false);
                debugCol.transform.localPosition = playerColliderOffset;
                float size = Mathf.Max(playerColliderSize.x, playerColliderSize.y, playerColliderSize.z);
                debugCol.transform.localScale = Vector3.one * size;
            }
            
            // Remove collider from debug visual so it doesn't interfere
            Destroy(debugCol.GetComponent<Collider>());
            
            // Material (Transparent Cyan)
            Renderer r = debugCol.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Sprites/Default"));
            r.material.color = new Color(0, 1, 1, 0.4f);
        }

        playerRoot.name = "Player";
        Player = playerRoot.GetComponent<PlayerController>();
        if (Player == null) Player = playerRoot.AddComponent<PlayerController>();
        
        // 3. Camera Config
        if (Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam == null) cam = Camera.main.gameObject.AddComponent<CameraFollow>();
            cam.target = Player.transform;
            cam.offset = new Vector3(0, 30, -20);
            
            // Snap Camera to the starting position/rotation so StartIntro captures the correct "Final" state
            cam.transform.position = Player.transform.position + cam.offset;
            cam.transform.position = Player.transform.position + cam.offset;
            cam.transform.LookAt(Player.transform.position);
        }

        // 4. PHYSICS COLLISION MATRIX
        // Layer 0: Default (Player, Enemies, Walls)
        // Layer 2: Ignore Raycast (Debris)
        // Layer 4: Water (Floor)
        
        // Debris (2) should IGNORE Default (0) -> Player/Enemies pass through
        Physics.IgnoreLayerCollision(0, 2, true);
        
        // Debris (2) should COLLIDE with Water (4) -> Bounces on floor
        Physics.IgnoreLayerCollision(4, 2, false);
    }

    void CreateEnvironment()
    {
        GameObject env = new GameObject("Environment");
        
        // Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "ArenaFloor";
        floor.layer = 4; // Set to 'Water' layer for Debris collision filtering
        floor.transform.parent = env.transform;
        floor.transform.localScale = new Vector3(cageAreaSize / 10f, 1, cageAreaSize / 10f);
        if (structureMaterial != null) floor.GetComponent<Renderer>().material = structureMaterial;
    }

    void CreateCage()
    {
        GameObject cage = new GameObject("Cage");
        CageGenerator gen = cage.AddComponent<CageGenerator>();
        
        gen.areaSize = this.cageAreaSize;
        gen.height = this.cageHeight;
        gen.verticalSpacing = this.cageVerticalSpacing;
        gen.horizontalSpacing = this.cageHorizontalSpacing;
        gen.barThickness = this.cageBarThickness;
        gen.cageMaterial = this.cageMaterial; // Pass material
        
        gen.GenerateCage();
    }

    void CreateCrowd()
    {
        GameObject crowd = new GameObject("Crowd");
        CrowdGenerator gen = crowd.AddComponent<CrowdGenerator>();
        
        gen.rows = this.crowdRows;
        gen.peoplePerRow = this.peoplePerRow;
        gen.spacing = this.crowdSpacing;
        gen.jumpSpeed = this.crowdJumpSpeed;
        gen.crowdMaterial = this.crowdBaseMaterial;
        gen.rowHeightDiff = this.crowdRowHeightDiff;
        
        // Generate on 4 sides
        float halfSize = (cageAreaSize / 2f) + 5f; // Offset from cage
        
        // Back (+Z)
        gen.GenerateBleachers(new Vector3(0, 0, halfSize), Vector3.back, cageAreaSize);
        // Front (-Z)
        gen.GenerateBleachers(new Vector3(0, 0, -halfSize), Vector3.forward, cageAreaSize);
        // Right (+X)
        gen.GenerateBleachers(new Vector3(halfSize, 0, 0), Vector3.left, cageAreaSize);
        // Left (-X)
        gen.GenerateBleachers(new Vector3(-halfSize, 0, 0), Vector3.right, cageAreaSize);
    }
}
