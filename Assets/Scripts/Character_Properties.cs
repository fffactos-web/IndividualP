using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Character_Properties : MonoBehaviour, IGemCollector
{
    [Header("Base Damage")]
    public float baseDamage;
    public float baseAttackSpeed;
    public float baseCritChance;
    public float baseCritDamage;
    public float baseArmorPenetration;

    [Header("Base Health")]
    public float baseMaxHealth;
    public float baseHealthRegen;

    [Header("Base Defense")]
    public float baseArmor;
    public float baseResistance;

    [Header("Base Sustain")]
    public float baseLifeSteal;

    [Header("Base Movement")]
    public float baseMoveSpeed;
    public float baseDashSpeed;
    public int baseExtraJumps;

    [Header("Base Effects")]
    public List<EffectEntry> activeEffects = new List<EffectEntry>();


    [Header("Base Area")]
    public float baseAttackRadius;

    [Header("Global")]
    public float globalDamageMultiplier;
    public float globalSpeedMultiplier;

    // ===== Bonuses (from items, buffs, etc.) =====

    [Header("Bonuses")]
    public float damageBonus;
    public float attackSpeedBonus;
    public float critChanceBonus;
    public float critDamageBonus;
    public float armorPenetrationBonus;

    public float maxHealthBonus;
    public float healthRegenBonus;

    public float armorBonus;
    public float resistanceBonus;

    public float lifeStealBonus;

    public float moveSpeedBonus;
    public float dashSpeedBonus;
    public int extraJumpsBonus;

    public float procChanceBonus;
    public float procPowerBonus;
    public int procCountBonus;

    public float attackRadiusBonus;

    // ===== Final calculated stats =====

    [HideInInspector] public float damage;
    [HideInInspector] public float attackSpeed;
    [HideInInspector] public float critChance;
    [HideInInspector] public float critDamage;
    [HideInInspector] public float armorPenetration;

    [HideInInspector] public float maxHealth;
    [HideInInspector] public float healthRegen;
    [HideInInspector] public float currentHealth;

    [HideInInspector] public float armor;
    [HideInInspector] public float resistance;

    [HideInInspector] public float lifeSteal;

    [HideInInspector] public float moveSpeed;
    [HideInInspector] public float dashSpeed;
    [HideInInspector] public int extraJumps;

    [HideInInspector] public float procChance;
    [HideInInspector] public float procPower;
    [HideInInspector] public int procCount;

    [HideInInspector] public float attackRadius;

    [Header("Meta")]
    public float kills;
    public float gems;
    public float difficulty;

    [SerializeField] GameObject[] dieEffect;
    [SerializeField] GameObject[] guns;
    [SerializeField] Transform gunHolder;
    [SerializeField] Transform camGunHolder;
    [SerializeField] DiePanel diePanel;
    [SerializeField] UnityEngine.UI.Slider[] healthBars;
    [SerializeField] UnityEngine.UI.Slider[] healthBarForeground;

    TextMeshProUGUI gemStatus;
    bool healthChanged;
    float timeWithoutHealthChanges;

    void Awake()
    {
        Instantiate(guns[0], gunHolder);
        Instantiate(guns[0], camGunHolder);

        RecalculateStats();
        currentHealth = maxHealth;

        ChangeGunProperties();

        gemStatus = GameObject.FindGameObjectWithTag("Gem Status")
            .GetComponent<TextMeshProUGUI>();

        foreach (var bar in healthBars)
        {
            bar.maxValue = maxHealth;
            bar.value = currentHealth;
        }
        foreach (var bar in healthBarForeground)
        {
            bar.maxValue = maxHealth;
            bar.value = currentHealth;
        }
    }

    public void RecalculateStats()
    {
        damage = (baseDamage + damageBonus) * (1 + globalDamageMultiplier);
        attackSpeed = (baseAttackSpeed + attackSpeedBonus) * (1 + globalSpeedMultiplier);

        critChance = Mathf.Clamp01(baseCritChance + critChanceBonus);
        critDamage = baseCritDamage + critDamageBonus;
        armorPenetration = baseArmorPenetration + armorPenetrationBonus;

        maxHealth = baseMaxHealth + maxHealthBonus;
        healthRegen = baseHealthRegen + healthRegenBonus;

        armor = baseArmor + armorBonus;
        resistance = baseResistance + resistanceBonus;

        lifeSteal = baseLifeSteal + lifeStealBonus;

        moveSpeed = (baseMoveSpeed + moveSpeedBonus) * (1 + globalSpeedMultiplier);
        dashSpeed = (baseDashSpeed + dashSpeedBonus) * (1 + globalSpeedMultiplier);
        extraJumps = baseExtraJumps + extraJumpsBonus;

        attackRadius = baseAttackRadius + attackRadiusBonus;
    }

    public void ChangeGunProperties()
    {
        var gun = gunHolder.GetComponentInChildren<Gun>();
        var camGun = camGunHolder.GetComponentInChildren<Gun>();

        if (gun == null || camGun == null)
        {
            Debug.LogWarning("Couldn't find gun instances under the configured holders.");
            return;
        }

        gun.effectData ??= new EffectData();
        camGun.effectData ??= new EffectData();

        gun.effectData.entries = activeEffects.ToArray();
        camGun.effectData.entries = activeEffects.ToArray();

        gun.SetOwner(this);
        camGun.SetOwner(this);
    }

    void Update()
    {
        if(currentHealth != maxHealth)
        {
            currentHealth = Mathf.Min(currentHealth + healthRegen * Time.deltaTime, maxHealth);
            foreach (var bar in healthBarForeground)
            {
                bar.maxValue = maxHealth;
                bar.value = currentHealth;
            }
        }

        timeWithoutHealthChanges += Time.deltaTime;
        if (healthChanged && timeWithoutHealthChanges > 1f)
        {
            foreach (var bar in healthBars)
                bar.DOValue(currentHealth, 1f);
            healthChanged = false;
        }
    }

    public void GetDamage(float dmg)
    {
        currentHealth -= dmg;
        healthChanged = true;
        timeWithoutHealthChanges = 0f;

        foreach (var bar in healthBarForeground)
            bar.value = currentHealth;

        if (currentHealth <= 0)
            Die();
    }

    public void AddGems(int amount)
    {
        gems += amount;
        gemStatus.text = gems.ToString();
    }

    void Die()
    {
        foreach (var effect in dieEffect)
            Instantiate(effect, transform.position + Vector3.up, Quaternion.identity);

        diePanel.showDiePanel();
        Cursor.lockState = CursorLockMode.None;
        gameObject.SetActive(false);
    }
}
