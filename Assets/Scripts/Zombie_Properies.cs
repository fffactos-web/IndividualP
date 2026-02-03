using UnityEngine;

public class Zombie_Properies : MonoBehaviour, IPoolable
{
    [Header("Health")]
    public float maxHealth;
    public float currentHealth;

    [Header("Damage popup")]
    [SerializeField] float stackResetTime = 0.5f;
    DamagePopup damagePopup;
    float lastDamageTime;

    public MobSpawner spawner;
    public GameObject dieEffect;

    Quaternion initialRotation;

    void Awake()
    {
        initialRotation = transform.rotation;
    }

    // ===== POOL =====

    public void OnSpawn()
    {
        ResetProperties();
        damagePopup = null;
        lastDamageTime = 0f;
    }

    public void OnDespawn()
    {
        if (damagePopup != null)
        {
            PoolManager.I.popupPool.Despawn(damagePopup.gameObject);
            damagePopup = null;
        }
        ResetProperties();
    }

    // ===== HEALTH =====

    void ResetProperties()
    {
        currentHealth = maxHealth;
        transform.rotation = initialRotation;
    }

    public void GetDamage(float damage)
    {
        currentHealth -= damage;

        ShowDamage(damage);

        if (currentHealth <= 0)
            Die();
    }

    // ===== DAMAGE STACK =====

    void ShowDamage(float damage)
    {
        if (PoolManager.I == null || PoolManager.I.popupPool == null)
        {
            Debug.LogError("PopupPool is NULL");
            return;
        }

        if (damagePopup == null || Time.time - lastDamageTime > stackResetTime)
        {
            var go = PoolManager.I.popupPool.Spawn(
                transform.position,
                Quaternion.identity
            );

            if (go == null)
            {
                Debug.LogError("Spawn returned NULL");
                return;
            }

            damagePopup = go.GetComponent<DamagePopup>();

            if (damagePopup == null)
            {
                Debug.LogError("DamagePopup component NOT FOUND on prefab");
                return;
            }

            damagePopup.Attach(transform);
        }

        damagePopup.AddDamage(damage);
        lastDamageTime = Time.time;
    }


    // ===== DEATH =====

    void Die()
    {
        if (damagePopup != null)
        {
            damagePopup.DetachAndFinish();
            damagePopup = null;
        }

        ResetProperties();

        PoolManager.I.deathEffectPool.Spawn(
            transform.position + Vector3.up * 0.8f,
            Quaternion.identity
        );

        PoolManager.I.followerZombiePool.Despawn(gameObject);
    }

}
