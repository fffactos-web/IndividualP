using TMPro;
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
    TextMeshProUGUI killsStatus;
    Character_Properties character;

    Quaternion initialLocalRotation;
    Vector3 initialLocalPosition;

    [SerializeField] private int baseGemCount = 2;
    [SerializeField] private int gemCountPerDifficulty = 2;

    [SerializeField] private float coneHeight = 1.2f;
    [SerializeField] private float coneRadius = 1f;

    void Awake()
    {
        killsStatus = GameObject.FindGameObjectWithTag("Kills Status").GetComponent<TextMeshProUGUI>();
        character = GameObject.FindGameObjectWithTag("Player").GetComponent<Character_Properties>();
        initialLocalRotation = transform.localRotation;
        initialLocalPosition = transform.localPosition;
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
        transform.localRotation = initialLocalRotation;
        transform.localPosition = initialLocalPosition;
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

        SpawnGems();

        character.kills++;
        killsStatus.text = (character.kills + 1).ToString();
        ResetProperties();

        PoolManager.I.deathEffectPool.Spawn(
            transform.position + Vector3.up * 0.8f,
            Quaternion.identity
        );

        // Zombie_Properies lives on a child of the pooled prefab. We must return the pooled root object.
        PoolManager.I.followerZombiePool.Despawn(transform.root.gameObject);
    }

    void SpawnGems()
    {
        int gemCount = baseGemCount + Mathf.RoundToInt(character.difficulty * gemCountPerDifficulty);

        for (int i = 0; i < gemCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(0f, coneRadius);
            float height = Random.Range(0.3f, coneHeight);

            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * radius,
                height,
                Mathf.Sin(angle) * radius
            );

            PoolManager.I.gemPool.Spawn(transform.position + offset,Quaternion.identity);
        }
    }


}
