using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ObjectPool : MonoBehaviour
{
    [SerializeField] GameObject prefab;
    public int preloadCount = 32;
    public bool isNavMeshAgent;

    Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        for (int i = 0; i < preloadCount; i++)
            Create();
    }

    public void Create()
    {
        if (!isNavMeshAgent)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);

            foreach (var auto in obj.GetComponentsInChildren<AutoReturnToPool>(true))
                auto.Init(this);

            pool.Enqueue(obj);
        }
    }

    public GameObject Spawn(Vector3 pos, Quaternion rot)
    {
        if (!isNavMeshAgent)
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
        else
        {
            if (pool.Count == 0)
                Create();

            GameObject obj = pool.Dequeue();

            obj.transform.SetParent(null);
            obj.GetComponent<NavMeshAgent>().Warp(pos);
            obj.SetActive(true);

            foreach (var p in obj.GetComponentsInChildren<IPoolable>(true))
                p.OnSpawn();

            return obj;
        }
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
