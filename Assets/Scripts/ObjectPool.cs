using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [SerializeField] GameObject prefab;
    [SerializeField] int preloadCount = 32;

    Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        for (int i = 0; i < preloadCount; i++)
            Create();
    }

    void Create()
    {
        GameObject obj = Instantiate(prefab);
        obj.SetActive(false);

        foreach (var auto in obj.GetComponentsInChildren<AutoReturnToPool>(true))
            auto.Init(this);

        pool.Enqueue(obj);
    }

    public GameObject Spawn(Vector3 pos, Quaternion rot)
    {
        if (pool.Count == 0)
            Create();

        GameObject obj = pool.Dequeue();

        obj.transform.SetParent(null);

        obj.transform.SetPositionAndRotation(pos, rot);
        obj.SetActive(true);

        foreach (var p in obj.GetComponentsInChildren<IPoolable>(true))
            p.OnSpawn();

        return obj;
    }


    public void Despawn(GameObject obj)
    {
        foreach (var p in obj.GetComponentsInChildren<IPoolable>(true))
            p.OnDespawn();

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pool.Enqueue(obj);
    }
}
