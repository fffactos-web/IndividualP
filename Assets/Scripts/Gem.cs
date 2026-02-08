using UnityEngine;
using DG.Tweening;

public class Gem : MonoBehaviour, IPoolable
{
    [SerializeField] private int value = 1;
    [SerializeField] private float flyTime = 2f;

    private Transform target;
    public Character_Properties c;
    private Tweener moveTween;
    private bool isFlying;
    private bool touched;

    public void OnSpawn()
    {
        isFlying = false;
        target = null;
        moveTween?.Kill();
    }

    public void OnDespawn()
    {
        moveTween?.Kill();
    }

    public void OnCompleteFly()
    {
        if(gameObject.activeInHierarchy)
            moveTween = transform.DOMove(target.position, flyTime).SetEase(Ease.InQuad);
    }

    public void FlyTo(Transform target, Character_Properties cc)
    {
        if (isFlying) return;

        touched = true;

        this.target = target;
        c = cc;
        isFlying = true;

        moveTween = transform.DOMove(target.position, flyTime).SetEase(Ease.InQuad).OnComplete(OnCompleteFly);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFlying) return;
        if (other.CompareTag("Player"))
        {
            c.AddGems(value);
            touched = false;
            PoolManager.I.gemPool.Despawn(gameObject);
        }
    }
}
