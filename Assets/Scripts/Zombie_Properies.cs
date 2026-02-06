using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private float coneRadius = 8f;

    [Header("Drops / Effects")]
    public EffectData onDamageEffects;

    Dictionary<int, Coroutine> activeDots = new Dictionary<int, Coroutine>();

    static int s_dotCounter = 0;
    static int NextDotId() => ++s_dotCounter;

    bool isDead;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void ApplyDot(int effectKey, float dps, float duration, float tickInterval, Character_Properties source, Poison.StackingMode stackingMode)
    {
        if (dps <= 0f || duration <= 0f) return;

        if (tickInterval <= 0f) tickInterval = 0.1f;
        if (tickInterval > duration) tickInterval = duration;

        if (stackingMode == Poison.StackingMode.Refresh)
        {
            if (activeDots.TryGetValue(effectKey, out Coroutine existing))
            {
                StopCoroutine(existing);
                activeDots.Remove(effectKey);
            }
            Coroutine c = StartCoroutine(DotCoroutine(effectKey, dps, duration, tickInterval, source));
            activeDots[effectKey] = c;
        }
        else if (stackingMode == Poison.StackingMode.Ignore)
        {
            if (activeDots.ContainsKey(effectKey)) return;
            Coroutine c = StartCoroutine(DotCoroutine(effectKey, dps, duration, tickInterval, source));
            activeDots[effectKey] = c;
        }
        else
        {
            int newKey = NextDotId();
            Coroutine c = StartCoroutine(DotCoroutine(newKey, dps, duration, tickInterval, source));
            activeDots[newKey] = c;
        }
    }

    IEnumerator DotCoroutine(int key, float dps, float duration, float tickInterval, Character_Properties source)
    {
        float remaining = duration;
        float dmgPerTick = dps * tickInterval;

        while (remaining > 0f && !isDead)
        {
            TakeDamage(dmgPerTick, null, false, source, ProcDamageType.DoT);

            remaining -= tickInterval;
            if (remaining <= 0f) break;
            yield return new WaitForSeconds(tickInterval);
        }

        activeDots.Remove(key);
    }

    public void TakeDamage(float damage, EffectData weaponEffect, bool isCrit = false, Character_Properties attacker = null, ProcDamageType damageType = ProcDamageType.Direct)
    {
        if (isDead || damage <= 0f) return;

        float healthBefore = currentHealth;
        bool wasAlive = healthBefore > 0f;

        float finalDamage = Mathf.Min(damage, Mathf.Max(0f, healthBefore));
        currentHealth = Mathf.Max(0f, healthBefore - damage);

        bool didKill = wasAlive && currentHealth <= 0f;

        ProcContext ctx = new ProcContext
        {
            damageDone = finalDamage,
            isCrit = isCrit,
            hitLayer = gameObject.layer,
            didKill = didKill,
            finalDamage = finalDamage,
            attacker = attacker,
            victim = this,
            damageType = damageType
        };

        ShowDamage(finalDamage);

        if (weaponEffect != null && ProcManager.Instance != null)
        {
            ProcManager.Instance.QueueProc(attacker, this, weaponEffect, ctx, EffectTriggerType.OnHit);
            if (didKill)
                ProcManager.Instance.QueueProc(attacker, this, weaponEffect, ctx, EffectTriggerType.OnKill);
        }

        if (attacker != null && attacker.activeEffects != null && attacker.activeEffects.Count > 0 && ProcManager.Instance != null)
        {
            EffectData wrapped = WrapEntries(attacker.activeEffects);
            ProcManager.Instance.QueueProc(attacker, this, wrapped, ctx, EffectTriggerType.OnHit);
            if (didKill)
                ProcManager.Instance.QueueProc(attacker, this, wrapped, ctx, EffectTriggerType.OnKill);
        }

        if (onDamageEffects != null && ProcManager.Instance != null)
            ProcManager.Instance.QueueProc(attacker, this, onDamageEffects, ctx, EffectTriggerType.OnDamaged);

        if (didKill)
            Die(ctx);
    }

    static EffectData WrapEntries(List<EffectEntry> list)
    {
        EffectData data = ScriptableObject.CreateInstance<EffectData>();
        data.entries = list.ToArray();
        return data;
    }

    public void InternalApplyDamage(float damage)
    {
        TakeDamage(damage, null, false, null, ProcDamageType.Proc);
    }

    public void OnSpawn()
    {
        ResetProperties();
        damagePopup = null;
        lastDamageTime = 0f;
        isDead = false;

        if (onDamageEffects != null && ProcManager.Instance != null)
        {
            ProcContext ctx = new ProcContext
            {
                victim = this,
                damageType = ProcDamageType.Proc
            };
            ProcManager.Instance.QueueProc(null, this, onDamageEffects, ctx, EffectTriggerType.OnSpawn);
        }
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
        transform.localRotation = initialLocalRotation;
        transform.localPosition = initialLocalPosition;
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

    void Die(ProcContext deathContext)
    {
        if (isDead) return;
        isDead = true;

        if (onDamageEffects != null && ProcManager.Instance != null)
            ProcManager.Instance.QueueProc(deathContext.attacker, this, onDamageEffects, deathContext, EffectTriggerType.OnDeath);

        if (damagePopup != null)
        {
            damagePopup.DetachAndFinish();
            damagePopup = null;
        }

        SpawnGems();

        if (character != null)
        {
            character.kills++;
            if (killsStatus != null)
                killsStatus.text = (character.kills + 1).ToString();
        }

        ResetProperties();

        foreach (var c in activeDots.Values)
            if (c != null) StopCoroutine(c);
        activeDots.Clear();

        PoolManager.I.deathEffectPool.Spawn(
            transform.position + Vector3.up * 0.8f,
            Quaternion.identity
        );

        PoolManager.I.followerZombiePool.Despawn(transform.root.gameObject);
    }

    void SpawnGems()
    {
        int gemCount = baseGemCount;
        if (character != null)
            gemCount += Mathf.RoundToInt(character.difficulty * gemCountPerDifficulty);

        Vector3 origin = transform.position + Vector3.up * 0.5f;

        for (int i = 0; i < gemCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(coneRadius / 2, coneRadius);

            Vector3 horizontal = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

            Vector3 targetPos = origin + horizontal;

            GameObject gem = PoolManager.I.gemPool.Spawn(origin, Quaternion.identity);

            gem.transform.DOJump(targetPos, Random.Range(2f, 4f), 1, Random.Range(1f, 2f), false).SetEase(Ease.OutQuad);
        }
    }
}
