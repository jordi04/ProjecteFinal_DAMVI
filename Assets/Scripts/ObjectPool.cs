using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    private static ObjectPool _instance;
    private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static GameObject GetPooledObject(GameObject prefab)
    {
        if (_instance == null)
        {
            GameObject poolObject = new GameObject("ObjectPool");
            _instance = poolObject.AddComponent<ObjectPool>();
        }

        int prefabID = prefab.GetInstanceID();

        if (!_instance.poolDictionary.ContainsKey(prefabID))
        {
            _instance.poolDictionary[prefabID] = new Queue<GameObject>();
            return null; // No pool for this prefab yet, will create a new instance
        }

        Queue<GameObject> objectPool = _instance.poolDictionary[prefabID];

        if (objectPool.Count == 0)
        {
            return null; // Pool exists but is empty, will create a new instance
        }

        GameObject pooledObject = objectPool.Dequeue();
        pooledObject.SetActive(true);
        return pooledObject;
    }

    public static void ReturnToPool(GameObject obj, GameObject prefab)
    {
        if (_instance == null) return;

        int prefabID = prefab.GetInstanceID();

        if (!_instance.poolDictionary.ContainsKey(prefabID))
        {
            _instance.poolDictionary[prefabID] = new Queue<GameObject>();
        }

        _instance.poolDictionary[prefabID].Enqueue(obj);
        obj.SetActive(false);
    }
}