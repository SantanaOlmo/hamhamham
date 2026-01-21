using System.Collections;
using UnityEngine;

public class GhostTrail : MonoBehaviour
{
    public float spawnInterval = 0.02f; // Faster spawn for smoother trail
    public float ghostDuration = 0.25f; // Fast fade to align with spawn stop
    // public Vector3 rotationFix = Vector3.zero; // REMOVED: Moved to GameManager

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

        string poolKey = "PlayerGhost";

        // Let's create a cached simple GameObject to serve as our "Prefab"
        if (_cachedGhostPrefab == null)
        {
            _cachedGhostPrefab = new GameObject(poolKey);
            _cachedGhostPrefab.AddComponent<MeshFilter>();
            _cachedGhostPrefab.AddComponent<MeshRenderer>();
            _cachedGhostPrefab.AddComponent<GhostObject>();
            _cachedGhostPrefab.SetActive(false); 
            DontDestroyOnLoad(_cachedGhostPrefab); 
        }

        // Calculate Rotation with Fix (FROM GAMEMANAGER)
        Vector3 rotFix = Vector3.zero;
        if (GameManager.Instance != null) rotFix = GameManager.Instance.dashRotation;

        Quaternion currentRot = playerRenderer.transform.rotation;
        Quaternion fixedRot = currentRot * Quaternion.Euler(rotFix);

        GameObject ghost = ObjectPoolManager.Instance.SpawnFromPool(_cachedGhostPrefab, playerRenderer.transform.position, fixedRot);
        
        GhostObject ghostScript = ghost.GetComponent<GhostObject>();
        if (ghostScript != null)
        {
            // Use the current player mesh and material
            // Use playerRenderer.transform.lossyScale to account for parent scaling + child scaling
            ghostScript.Init(playerMeshFilter.mesh, playerRenderer.transform.position, fixedRot, playerRenderer.transform.lossyScale, playerRenderer.material, ghostDuration);
        }
    }

    private static GameObject _cachedGhostPrefab;
}
