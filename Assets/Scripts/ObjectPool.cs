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

        if (obj.TryGetComponent(out AutoReturnToPool auto))
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

        if (obj.TryGetComponent(out IPoolable p))
            p.OnSpawn();

        return obj;
    }


    public void Despawn(GameObject obj)
    {
        if (obj.TryGetComponent(out IPoolable p))
            p.OnDespawn();

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pool.Enqueue(obj);
    }
}
