using UnityEngine;

public class Rocket : MonoBehaviour, IPoolable
{
    Rigidbody rb;
    Camera cam;

    [Header("Damage")]
    public float radius = 1f;
    public float dmg = 10f;

    [Header("Movement")]
    public float speed = 60f;

    [Header("References")]
    RectTransform crosshair;

    bool canExplode;

    public EffectData effectData;
    Character_Properties owner;

    public void SetOwner(Character_Properties character)
    {
        owner = character;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;

        crosshair = GameObject.FindGameObjectWithTag("Crosshair")
            .GetComponent<RectTransform>();
    }

    public void OnSpawn()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(null, crosshair.position);

        Ray ray = cam.ScreenPointToRay(screenPoint);
        Vector3 dir = ray.direction.normalized;

        Vector3 modelForwardOffset = Vector3.left;

        transform.rotation = Quaternion.LookRotation(dir, Vector3.up)
                             * Quaternion.FromToRotation(modelForwardOffset, Vector3.forward);

        rb.AddForce(dir * speed, ForceMode.Impulse);

        canExplode = false;
        Invoke(nameof(EnableExplosion), 0.05f);
    }

    void EnableExplosion()
    {
        canExplode = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canExplode) return;
        if (other.isTrigger) return;
        if (other.CompareTag("Player")) return;

        Explode();
    }

    void Explode()
    {
        PoolManager.I.explosionPool
            .Spawn(transform.position, transform.rotation);

        foreach (var col in Physics.OverlapSphere(transform.position, radius))
        {
            col.GetComponent<Zombie_Properies>()?.TakeDamage(dmg, effectData, false, null, ProcDamageType.Explosion);
        }

        PoolManager.I.rocketsPool.Despawn(gameObject);
    }

    public void OnDespawn()
    {
        CancelInvoke();
        canExplode = false;
    }
}
