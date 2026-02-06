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

    void Awake()
    {
        currentHealth = maxHealth;
    }

    // effectKey  êëþ÷ ýôôåêòà (îáû÷íî GetInstanceID() àññåòà)
    public void ApplyDot(int effectKey, float dps, float duration, float tickInterval, Character_Properties source, Poison.StackingMode stackingMode)
    {
        if (dps <= 0f || duration <= 0f) return;

        // Çàùèòíûå ìåðû
        if (tickInterval <= 0f) tickInterval = 0.1f;
        if (tickInterval > duration) tickInterval = duration;

        if (stackingMode == Poison.StackingMode.Refresh)
        {
            // åñëè óæå åñòü  ïåðåçàïóñêàåì êîðóòèíó (îáíîâëÿåì äëèòåëüíîñòü)
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
        else // Stack
        {
            int newKey = NextDotId();
            Coroutine c = StartCoroutine(DotCoroutine(newKey, dps, duration, tickInterval, source));
            activeDots[newKey] = c;
        }
    }

    IEnumerator DotCoroutine(int key, float dps, float duration, float tickInterval, Character_Properties source)
    {
        float remaining = duration;
        // óðîí â êàæäîì òèêå
        float dmgPerTick = dps * tickInterval;

        while (remaining > 0f)
        {
            // íàíîñÿ óðîí, èñïîëüçóåì âíóòðåííèé âûçîâ, ÷òîáû íå ñòàâèòü íîâûå proc'û è íå ââîäèòü ðåêóðñèþ
            ApplyDamageWithProcs(dmgPerTick, null, false, ProcTrigger.OnDamaged);

            remaining -= tickInterval;
            if (remaining <= 0f) break;
            yield return new WaitForSeconds(tickInterval);
        }

        // óäàëÿåì çàïèñü
        activeDots.Remove(key);
    }


    // Ýòîò ìåòîä äîëæåí âûçûâàòüñÿ êîãäà íàíîñèòñÿ óðîí èçâíå (íàïðèìåð èç Gun.cs)
    // source  weaponEffect  EffectData îò îðóæèÿ (åñëè åñòü)
    public void TakeDamage(float damage, EffectData weaponEffect, bool isCrit = false)
    {
        ApplyDamageWithProcs(damage, weaponEffect, isCrit, ProcTrigger.OnHit);
    }

    static EffectData WrapEntries(List<EffectEntry> list)
    {
        EffectData data = ScriptableObject.CreateInstance<EffectData>();
        data.entries = list.ToArray();
        return data;
    }

    void ApplyDamageWithProcs(float damage, EffectData weaponEffect, bool isCrit, ProcTrigger incomingTrigger)
    {
        InternalApplyDamage(damage);

        if (ProcManager.Instance == null) return;

        ProcContext incomingCtx = new ProcContext
        {
            trigger = incomingTrigger,
            damageDone = damage,
            isCrit = isCrit,
            hitLayer = gameObject.layer
        };

        if (weaponEffect != null)
            ProcManager.Instance.QueueProc(this, weaponEffect, incomingCtx);

        if (character != null && character.activeEffects != null && character.activeEffects.Count > 0)
            ProcManager.Instance.QueueProc(this, WrapEntries(character.activeEffects), incomingCtx);

        if (onDamageEffects != null)
        {
            ProcContext damagedCtx = incomingCtx;
            damagedCtx.trigger = ProcTrigger.OnDamaged;
            ProcManager.Instance.QueueProc(this, onDamageEffects, damagedCtx);
        }
    }


    // Âíóòðåííåå ïðèìåíåíèå çäîðîâüÿ  íåáîëüøàÿ âûäåëåííàÿ ôóíêöèÿ, ÷òîáû EffectAction ìîã íàïðÿìóþ íàíåñòè äîï. óðîí
    public void InternalApplyDamage(float damage)
    {
        currentHealth -= damage;
        // òóò ìîæåøü äîáàâèòü âñïëûâàþùóþ íàäïèñü óðîíà, çâóê, àíèìàöèþ
        if (currentHealth <= 0f)
        {
            Die();
        }
        ShowDamage(damage);
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

    // ===== HEALTH =====

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
        int gemCount = baseGemCount + Mathf.RoundToInt(character.difficulty * gemCountPerDifficulty);

        Vector3 origin = transform.position + Vector3.up * 0.5f;

        for (int i = 0; i < gemCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(coneRadius/2, coneRadius);

            Vector3 horizontal = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

            Vector3 targetPos = origin + horizontal;

            GameObject gem = PoolManager.I.gemPool.Spawn(origin, Quaternion.identity);

            gem.transform.DOJump(targetPos, Random.Range(2f, 4f), 1, Random.Range(1f, 2f), false).SetEase(Ease.OutQuad);
        }
    }
}
