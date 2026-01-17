using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;

    // Dictionary to hold pools based on unique "Prefab Name" or Tag
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
    
    // Additional Dictionary to track which prefab does a GameObj belong to (for automatic return)
    // Or we just rely on passing the "Tag" string manually.
    // For simplicity, we will expect the user to pass the Prefab reference to Spawn, 
    // and we use the Prefab.name as key.

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        
        string key = prefab.name;

        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary.Add(key, new Queue<GameObject>());
        }

        GameObject objToSpawn = null;
        
        // Try to dequeue valid objects
        while (poolDictionary[key].Count > 0)
        {
            GameObject candidate = poolDictionary[key].Dequeue();
            if (candidate != null) 
            {
                objToSpawn = candidate;
                break;
            }
        }

        // If nothing found, create new
        if (objToSpawn == null)
        {
            objToSpawn = Instantiate(prefab);
            objToSpawn.name = key; // Keep name clean to help identification? Or allow (Clone)
            // It's safer to keep it consistent if needed, but Instantiate adds (Clone).
            // Usually we don't rely on .name for logic.
        }

        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        objToSpawn.SetActive(true);

        return objToSpawn;
    }

    public void ReturnToPool(GameObject obj, string prefabKey) // prefabKey optional if we could lookup
    {
        if (obj == null) return;
        
        obj.SetActive(false);
        
        // If key (prefab name) not provided, we might have an issue.
        // For this simple implementation, let's assume we clean the name:
        string key = obj.name.Replace("(Clone)", "").Trim();

        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary.Add(key, new Queue<GameObject>());
        }

        poolDictionary[key].Enqueue(obj);
    }
}
