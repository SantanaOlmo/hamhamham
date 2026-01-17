using System.Collections;
using UnityEngine;

public class GhostTrail : MonoBehaviour
{
    public float spawnInterval = 0.02f; // Faster spawn for smoother trail
    public float ghostDuration = 0.25f; // Fast fade to align with spawn stop

    private Coroutine trailCoroutine;
    private MeshFilter playerMeshFilter;
    private Renderer playerRenderer;

    void Start()
    {
        playerMeshFilter = GetComponent<MeshFilter>();
        if (playerMeshFilter == null) playerMeshFilter = GetComponentInChildren<MeshFilter>();

        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer == null) playerRenderer = GetComponentInChildren<Renderer>();
    }

    public void StartTrail()
    {
        if (trailCoroutine != null) StopCoroutine(trailCoroutine);
        trailCoroutine = StartCoroutine(SpawnTrailRoutine());
    }

    public void StopTrail()
    {
        if (trailCoroutine != null) StopCoroutine(trailCoroutine);
        trailCoroutine = null;
    }

    IEnumerator SpawnTrailRoutine()
    {
        SpawnGhost(); // Spawn immediately at start position
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnGhost();
        }
    }

    void SpawnGhost()
    {
        if (playerMeshFilter == null || playerRenderer == null) return;
        if (ObjectPoolManager.Instance == null) return;

        // key for the pool
        string poolKey = "PlayerGhost";

        // Ideally we ask the pool for an object. 
        // But our pool manager takes a prefab to instantiate if empty.
        // We need a "Ghost Prefab". 
        // Since we don't have one on disk, we can't easily pass it to SpawnFromPool 
        // IF the pool is empty and needs to create new.
        
        // WORKAROUND: We will check if we can get one. 
        // If not, we create a temporary inactive dummy to pass as "prefab" 
        // OR we modify ObjectPoolManager to handle this.
        // Let's modify ObjectPoolManager slightly to be more robust, 
        // OR simpler: Create a runtime prefab once.
        
        // Actually, let's just create a new GameObject in SpawnFromPool if prefab is null? 
        // No, `SpawnFromPool` requires a prefab.
        
        // Let's create a cached simple GameObject to serve as our "Prefab"
        if (_cachedGhostPrefab == null)
        {
            _cachedGhostPrefab = new GameObject(poolKey);
            _cachedGhostPrefab.AddComponent<MeshFilter>();
            _cachedGhostPrefab.AddComponent<MeshRenderer>();
            _cachedGhostPrefab.AddComponent<GhostObject>();
            _cachedGhostPrefab.SetActive(false); // Prefabs should be inactive usually
            // Don't destroy this, keep it in scene or DontDestroyOnLoad
            DontDestroyOnLoad(_cachedGhostPrefab); 
        }

        // Use the renderer's transform to ensure we capture the visual's offset/rotation correctly
        GameObject ghost = ObjectPoolManager.Instance.SpawnFromPool(_cachedGhostPrefab, playerRenderer.transform.position, playerRenderer.transform.rotation);
        
        GhostObject ghostScript = ghost.GetComponent<GhostObject>();
        if (ghostScript != null)
        {
            // Use the current player mesh and material
            // Use playerRenderer.transform.lossyScale to account for parent scaling + child scaling
            ghostScript.Init(playerMeshFilter.mesh, playerRenderer.transform.position, playerRenderer.transform.rotation, playerRenderer.transform.lossyScale, playerRenderer.material, ghostDuration);
        }
    }

    private static GameObject _cachedGhostPrefab;
}
