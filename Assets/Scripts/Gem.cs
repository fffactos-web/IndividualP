using UnityEngine;

public class Gem : MonoBehaviour, IPoolable
{
    [SerializeField] private int value = 1;
    [SerializeField] private float flyTime = 2f;
    [SerializeField] private float pickupDistance = 0.25f;

    private Transform target;
    public Character_Properties c;
    private bool isFlying;
    private bool touched;
    private float flySpeed;

    public void OnSpawn()
    {
        isFlying = false;
        target = null;
        touched = false;
    }

    public void OnDespawn()
    {
        isFlying = false;
        target = null;
    }

    private void Update()
    {
        if (!isFlying || target == null) return;

        transform.position = Vector3.MoveTowards(transform.position, target.position, flySpeed * Time.deltaTime);

        if ((transform.position - target.position).sqrMagnitude <= pickupDistance * pickupDistance)
            Collect();
    }

    public void FlyTo(Transform target, Character_Properties cc)
    {
        if (isFlying) return;

        touched = true;

        this.target = target;
        c = cc;
        isFlying = true;
        flySpeed = Vector3.Distance(transform.position, target.position) / Mathf.Max(flyTime, 0.01f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFlying) return;
        if (other.CompareTag("Player"))
            Collect();
    }

    private void Collect()
    {
        if (!isFlying || c == null) return;

        isFlying = false;

        if (!touched) return;

        touched = false;
        c.AddGems(value);
        if (PoolManager.I != null && PoolManager.I.gemPool != null)
        {
            PoolManager.I.gemPool.Despawn(gameObject);
        }
    }
}
