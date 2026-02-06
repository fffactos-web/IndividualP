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

    [SerializeField] int baseGemCount = 2;
    [SerializeField] int gemCountPerDifficulty = 2;

    [SerializeField] float coneRadius = 8f;

    [Header("Drops / Effects")]
    public EffectData onDamageEffects;

    readonly Dictionary<int, Coroutine> activeDots = new Dictionary<int, Coroutine>();

    static int s_dotCounter = 0;
    static int NextDotId() => ++s_dotCounter;

    bool isDead;

    void Awake()
    {
        currentHealth = maxHealth;
        initialLocalRotation = transform.localRotation;
        initialLocalPosition = transform.localPosition;

        GameObject player = GameObject.FindGameObjectWithTag("Character");
        if (player != null)
            character = player.GetComponent<Character_Properties>();

        GameObject killsObj = GameObject.FindGameObjectWithTag("Kills Status");
        if (killsObj != null)
            killsStatus = killsObj.GetComponent<TextMeshProUGUI>();
    }

    public void ApplyDot(int effectKey, float dps, float duration, float tickInterval, Character_Properties source, Poison.StackingMode stackingMode)
    {
        if (dps <= 0f || duration <= 0f)
            return;

        if (tickInterval <= 0f)
            tickInterval = 0.1f;

        if (tickInterval > duration)
            tickInterval = duration;

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
            if (activeDots.ContainsKey(effectKey))
                return;

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
            TakeDamage(dmgPerTick, null, false, source, ProcDamageType.DamageOverTime);

            remaining -= tickInterval;
            if (remaining <= 0f)
                break;

            yield return new WaitForSeconds(tickInterval);
        }

        activeDots.Remove(key);
    }

    public void TakeDamage(float damage, EffectData weaponEffect, bool isCrit = false, Character_Properties attacker = null, ProcDamageType damageType = ProcDamageType.Direct)
    {
        if (isDead || damage <= 0f)
            return;

        float hpBefore = currentHealth;
        InternalApplyDamage(damage);

        bool didKill = isDead;

        ProcContext onHitCtx = BuildContext(
            trigger: ProcTrigger.OnHit,
            damageDone: damage,
            isCrit: isCrit,
            hpBefore: hpBefore,
            attacker: attacker,
            damageType: damageType,
            targetWasKilled: didKill
        );

        if (ProcManager.Instance == null)
        {
            if (didKill)
                Die(onHitCtx);
            return;
        }

        if (weaponEffect != null)
            ProcManager.Instance.QueueProc(attacker, this, weaponEffect, onHitCtx);

        if (attacker != null && attacker.activeEffects != null && attacker.activeEffects.Count > 0)
            ProcManager.Instance.QueueProc(attacker, this, attacker.activeEffects, attacker.GetInstanceID(), onHitCtx);

        if (onDamageEffects != null)
        {
            ProcContext damagedCtx = onHitCtx;
            damagedCtx.trigger = ProcTrigger.OnDamaged;
            ProcManager.Instance.QueueProc(attacker, this, onDamageEffects, damagedCtx);
        }

        if (didKill)
        {
            ProcContext onKillCtx = onHitCtx;
            onKillCtx.trigger = ProcTrigger.OnKill;

            if (weaponEffect != null)
                ProcManager.Instance.QueueProc(attacker, this, weaponEffect, onKillCtx);

            if (attacker != null && attacker.activeEffects != null && attacker.activeEffects.Count > 0)
                ProcManager.Instance.QueueProc(attacker, this, attacker.activeEffects, attacker.GetInstanceID(), onKillCtx);

            Die(onKillCtx);
        }
    }

    ProcContext BuildContext(ProcTrigger trigger, float damageDone, bool isCrit, float hpBefore, Character_Properties attacker, ProcDamageType damageType, bool targetWasKilled)
    {
        return new ProcContext
        {
            trigger = trigger,
            damageDone = damageDone,
            isCrit = isCrit,
            hitLayer = gameObject.layer,
            targetWasKilled = targetWasKilled,
            targetHealthBefore = hpBefore,
            targetHealthAfter = currentHealth,
            hitPosition = transform.position,
            attacker = attacker,
            victim = this,
            damageType = damageType
        };
    }

    public void InternalApplyDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        ShowDamage(damage);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            isDead = true;
        }
    }

    public void OnSpawn()
    {
        ResetProperties();
        damagePopup = null;
        lastDamageTime = 0f;
        isDead = false;

        if (onDamageEffects != null && ProcManager.Instance != null)
        {
            ProcContext ctx = BuildContext(ProcTrigger.OnSpawn, 0f, false, currentHealth, null, ProcDamageType.Proc, false);
            ProcManager.Instance.QueueProc(null, this, onDamageEffects, ctx);
        }
    }

    public void OnDespawn()
    {
        if (damagePopup != null)
        {
            PoolManager.I.popupPool.Despawn(damagePopup.gameObject);
            damagePopup = null;
        }

        foreach (var c in activeDots.Values)
            if (c != null)
                StopCoroutine(c);

        activeDots.Clear();
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
            GameObject go = PoolManager.I.popupPool.Spawn(transform.position, Quaternion.identity);
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
        if (onDamageEffects != null && ProcManager.Instance != null)
        {
            ProcContext onDeathCtx = deathContext;
            onDeathCtx.trigger = ProcTrigger.OnDeath;
            onDeathCtx.targetWasKilled = true;
            ProcManager.Instance.QueueProc(deathContext.attacker, this, onDamageEffects, onDeathCtx);
        }

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
                killsStatus.text = character.kills.ToString();
        }

        foreach (var c in activeDots.Values)
            if (c != null)
                StopCoroutine(c);

        activeDots.Clear();

        if (PoolManager.I != null && PoolManager.I.deathEffectPool != null)
            PoolManager.I.deathEffectPool.Spawn(transform.position + Vector3.up * 0.8f, Quaternion.identity);

        if (PoolManager.I != null && PoolManager.I.followerZombiePool != null)
            PoolManager.I.followerZombiePool.Despawn(transform.root.gameObject);
    }

    void SpawnGems()
    {
        if (PoolManager.I == null || PoolManager.I.gemPool == null)
            return;

        int gemCount = baseGemCount;
        if (character != null)
            gemCount += Mathf.RoundToInt(character.difficulty * gemCountPerDifficulty);

        Vector3 origin = transform.position + Vector3.up * 0.5f;

        for (int i = 0; i < gemCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(coneRadius / 2f, coneRadius);
            Vector3 targetPos = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

            GameObject gem = PoolManager.I.gemPool.Spawn(origin, Quaternion.identity);
            gem.transform.DOJump(targetPos, Random.Range(2f, 4f), 1, Random.Range(1f, 2f), false).SetEase(Ease.OutQuad);
        }
    }
}
