using DG.Tweening;
using TMPro;
using UnityEngine;

public class Zombie_Properies : MonoBehaviour, IPoolable
{
    public struct HitData
    {
        public float rawDamage;
        public float armorPenetration;
        public float critMultiplier;
        public float statusChance;
        public float statusDuration;
        public float procChance;
        public float procPower;
        public int procCount;
    }

    [Header("Health")]
    public float maxHealth;
    public float currentHealth;

    [Header("Defense")]
    public float armor = 0f;
    [Range(0f, 0.95f)] public float resistance = 0f;

    [Header("Status")]
    public bool hasStatus;
    public float statusTimer;

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

    [SerializeField] private float coneHeight = 2f;
    [SerializeField] private float coneRadius = 2f;

    void Awake()
    {
        killsStatus = GameObject.FindGameObjectWithTag("Kills Status").GetComponent<TextMeshProUGUI>();
        character = GameObject.FindGameObjectWithTag("Player").GetComponent<Character_Properties>();
        initialLocalRotation = transform.localRotation;
        initialLocalPosition = transform.localPosition;
    }

    void Update()
    {
        if (hasStatus)
        {
            statusTimer -= Time.deltaTime;
            if (statusTimer <= 0f)
            {
                statusTimer = 0f;
                hasStatus = false;
            }
        }
    }

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

    void ResetProperties()
    {
        currentHealth = maxHealth;
        hasStatus = false;
        statusTimer = 0f;
        transform.localRotation = initialLocalRotation;
        transform.localPosition = initialLocalPosition;
    }

    public void GetDamage(float damage)
    {
        TakeHit(new HitData { rawDamage = damage, critMultiplier = 1f, procCount = 1, procPower = 1f });
    }

    public float TakeHit(HitData hitData)
    {
        float effectiveArmor = Mathf.Max(0f, armor - hitData.armorPenetration);
        float armorMultiplier = 100f / (100f + effectiveArmor);
        float damageAfterMitigation = hitData.rawDamage * Mathf.Max(1f, hitData.critMultiplier) * armorMultiplier * (1f - resistance);

        if (hasStatus && character != null)
            damageAfterMitigation *= 1f + character.GetStats().damageVsStatusTargets;

        if (hitData.procCount > 0 && hitData.procChance > 0f)
        {
            for (int i = 0; i < hitData.procCount; i++)
            {
                if (Random.value <= hitData.procChance)
                    damageAfterMitigation += hitData.rawDamage * Mathf.Max(0f, hitData.procPower - 1f);
            }
        }

        currentHealth -= damageAfterMitigation;
        ShowDamage(damageAfterMitigation);

        if (Random.value <= hitData.statusChance)
        {
            hasStatus = true;
            statusTimer = Mathf.Max(statusTimer, hitData.statusDuration);
        }

        if (currentHealth <= 0)
            Die();

        return Mathf.Max(0f, damageAfterMitigation);
    }

    void ShowDamage(float damage)
    {
        if (PoolManager.I == null || PoolManager.I.popupPool == null)
        {
            Debug.LogError("PopupPool is NULL");
            return;
        }

        if (damagePopup == null || Time.time - lastDamageTime > stackResetTime)
        {
            var go = PoolManager.I.popupPool.Spawn(transform.position, Quaternion.identity);
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

    void Die()
    {
        if (damagePopup != null)
        {
            damagePopup.DetachAndFinish();
            damagePopup = null;
        }

        SpawnGems();
        SpawnExpirience();

        character.kills++;
        killsStatus.text = character.kills.ToString();
        ResetProperties();

        PoolManager.I.deathEffectPool.Spawn(transform.position + Vector3.up * 0.8f, Quaternion.identity);
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

            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            PoolManager.I.gemPool.Spawn(transform.position + offset, Quaternion.identity);
        }
    }

    void SpawnExpirience()
    {
        int gemCount = baseGemCount + Mathf.RoundToInt(character.difficulty * gemCountPerDifficulty);

        for (int i = 0; i < gemCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(coneRadius, coneRadius * 2);
            Vector3 groundOffset = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            Vector3 startPos = transform.position + Vector3.up * 0.6f;
            Vector3 endPos = transform.position + groundOffset;

            GameObject gem = PoolManager.I.expiriencePool.Spawn(startPos, Quaternion.identity);

            float jumpPower = Random.Range(coneHeight, coneHeight * 2);
            float duration = Random.Range(.5f, 1f);

            gem.transform.DOJump(endPos, jumpPower, 1, duration).SetEase(Ease.OutQuad);
        }
    }

}
