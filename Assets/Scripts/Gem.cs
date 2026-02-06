using UnityEngine;
using DG.Tweening;

public class Gem : MonoBehaviour, IPoolable
{
    [SerializeField] private int value = 1;
    [SerializeField] private float flyTime = 1f;

    private Transform target;
    private IGemCollector collector;
    private Tweener moveTween;
    private bool isFlying;

    public void OnSpawn()
    {
        isFlying = false;
        target = null;
        collector = null;
        moveTween?.Kill();
    }

    public void OnDespawn()
    {
        moveTween?.Kill();
    }

    public void FlyTo(Transform target, IGemCollector collector)
    {
        if (isFlying) return;

        this.target = target;
        this.collector = collector;
        isFlying = true;

        moveTween = transform.DOMove(target.position, flyTime).SetEase(Ease.InQuad);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!isFlying) return;

            if (collector != null)
                collector.AddGems(value);

            PoolManager.I.gemPool.Despawn(gameObject);
        }
    }
}
