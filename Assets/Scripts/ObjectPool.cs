using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [SerializeField] GameObject prefab;
    [SerializeField] int preloadCount = 32;

    [SerializeField] bool isUI;
    [SerializeField] Transform uiParent;

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

        if (isUI)
            obj.transform.SetParent(uiParent, false);
        else
            obj.transform.SetParent(null);

        obj.transform.SetPositionAndRotation(pos, rot);
        obj.SetActive(true);

        // A pooled prefab can have poolable logic on children (common with imported rigs).
        // Calling only TryGetComponent on root would miss those components.
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
