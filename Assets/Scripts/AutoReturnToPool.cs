using UnityEngine;
using System.Collections;

public class AutoReturnToPool : MonoBehaviour, IPoolable
{
    public float lifeTime = 1f;

    ObjectPool pool;

    public void Init(ObjectPool p)
    {
        pool = p;
    }

    public void OnSpawn()
    {
        StopAllCoroutines();
        StartCoroutine(Return());
    }

    IEnumerator Return()
    {
        yield return new WaitForSeconds(lifeTime);

        if (pool != null)
        {
            pool.Despawn(gameObject);
        }
        else
        {
            Debug.LogWarning(
                $"{name} returned without pool, disabling manually"
            );
            gameObject.SetActive(false);
        }
    }

    public void OnDespawn() { }
}
