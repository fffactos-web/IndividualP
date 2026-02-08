using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

public class Character_Properties : MonoBehaviour
{
    [Serializable]
    public class HeroStats
    {
        [Header("Offense")]
        public float damage = 4f;
        public float attackSpeed = 1f;
        [Range(0f, 1f)] public float critChance = 0.1f;
        public float critDamageMultiplier = 2f;
        public float armorPenetration = 0f;
        public float globalDamageMultiplier = 1f;
        public float damageVsStatusTargets = 0f;
        public float missingHealthDamage = 0f;
        public float lowHealthPower = 0f;

        [Header("Defense")]
        public float maxHealth = 100f;
        public float healthRegen = 0f;
        public float shield = 0f;
        public float maxShield = 0f;
        public float armor = 0f;
        [Range(0f, 0.95f)] public float resistance = 0f;
        public float lifesteal = 0f;

        [Header("Movement")]
        public float moveSpeed = 1f;
        public float dashSpeed = 1f;
        public int jumpCount = 1;
        [Range(0f, 1f)] public float airControl = 0.35f;
        public float globalAcceleration = 1f;

        [Header("Cooldowns / cast")]
        [Range(0f, 0.8f)] public float cooldownReduction = 0f;
        public float castSpeed = 1f;

        [Header("Skill resources")]
        public float skillResource = 100f;
        public float maxSkillResource = 100f;
        public float resourceRegen = 10f;

        [Header("Procs")]
        [Range(0f, 1f)] public float procChance = 0f;
        public float procPower = 1f;
        public int procCount = 1;

        [Header("Statuses")]
        [Range(0f, 1f)] public float statusChance = 0f;
        public float statusDuration = 1f;

        [Header("Meta modifiers")]
        public float killBonus = 0f;
        public float killStreakBonus = 0f;
        public float onHitTakenEffectPower = 0f;
        public float statExchange = 0f;

        [Header("AoE / ranges")]
        public float attackRadius = 1f;
        public float skillRange = 1f;
        public float abilityHitboxSize = 1f;

        [Header("Inventory")]
        public int itemSlotLimit = 6;
    }

    [Header("References")]
    [SerializeField] GameObject[] dieEffect;
    [SerializeField] GameObject[] guns;
    [SerializeField] DiePanel diePanel;
    [SerializeField] UnityEngine.UI.Slider[] healthBars;
    [SerializeField] UnityEngine.UI.Slider[] healthBarForeground;
    [SerializeField] UnityEngine.UI.Slider expirienceBar;
    [SerializeField] Transform gunHolder;
    [SerializeField] Transform camGunHolder;

    [Header("Stats")]
    [SerializeField] HeroStats baseStats = new HeroStats();
    [SerializeField] HeroStats bonusStats = CreateBonusStats();

    [Header("Progression")]
    [SerializeField] int startExperienceForNextLevel = 20;
    [SerializeField] float experienceGrowthMultiplier = 2f;

    float currentHealth;
    float currentShield;
    float currentSkillResource;

    TextMeshProUGUI gemStatus;

    bool healthChanged;
    float timeWithoutHealthChanhges = 0f;

    public float difficulty = 1;
    public float kills;
    public float gems;

    public int level { get; private set; } = 1;
    [SerializeField]
    public int currentExperience { get; private set; }
    public int experienceForNextLevel { get; private set; }

    public event Action OnLevelUp;

    static HeroStats CreateBonusStats()
    {
        return new HeroStats
        {
            damage = 0f,
            attackSpeed = 0f,
            critChance = 0f,
            critDamageMultiplier = 0f,
            armorPenetration = 0f,
            globalDamageMultiplier = 0f,
            maxHealth = 0f,
            healthRegen = 0f,
            shield = 0f,
            maxShield = 0f,
            armor = 0f,
            resistance = 0f,
            lifesteal = 0f,
            moveSpeed = 0f,
            dashSpeed = 0f,
            jumpCount = 0,
            airControl = 0f,
            globalAcceleration = 0f,
            cooldownReduction = 0f,
            castSpeed = 0f,
            skillResource = 0f,
            maxSkillResource = 0f,
            resourceRegen = 0f,
            procChance = 0f,
            procPower = 0f,
            procCount = 0,
            statusChance = 0f,
            statusDuration = 0f,
            damageVsStatusTargets = 0f,
            killBonus = 0f,
            killStreakBonus = 0f,
            onHitTakenEffectPower = 0f,
            missingHealthDamage = 0f,
            lowHealthPower = 0f,
            statExchange = 0f,
            attackRadius = 0f,
            skillRange = 0f,
            abilityHitboxSize = 0f,
            itemSlotLimit = 0
        };
    }

    private void Awake()
    {
        Instantiate(guns[0], camGunHolder);
        Instantiate(guns[0], gunHolder);

        ResetProperties();
        ChangeGunProperties();

        diePanel.gameObject.SetActive(false);
        gemStatus = GameObject.FindGameObjectWithTag("Gem Status").GetComponent<TextMeshProUGUI>();
        experienceForNextLevel = Mathf.Max(1, startExperienceForNextLevel);
        expirienceBar.maxValue = experienceForNextLevel;
        expirienceBar.value = currentExperience;

        UpdateHealthBars();
        UpdateGemStatus();
    }

    private void Update()
    {
        timeWithoutHealthChanhges += Time.deltaTime;
        RegenerateHealthAndResource();

        if (healthChanged && timeWithoutHealthChanhges > 1f)
        {
            foreach (var bar in healthBars)
                bar.DOValue(currentHealth, 1f);

            timeWithoutHealthChanhges = 0f;
            healthChanged = false;
        }
    }

    public HeroStats GetStats()
    {
        HeroStats s = new HeroStats
        {
            damage = baseStats.damage + bonusStats.damage,
            attackSpeed = Mathf.Max(0.05f, baseStats.attackSpeed + bonusStats.attackSpeed),
            critChance = Mathf.Clamp01(baseStats.critChance + bonusStats.critChance),
            critDamageMultiplier = Mathf.Max(1f, baseStats.critDamageMultiplier + bonusStats.critDamageMultiplier),
            armorPenetration = Mathf.Max(0f, baseStats.armorPenetration + bonusStats.armorPenetration),
            globalDamageMultiplier = Mathf.Max(0f, baseStats.globalDamageMultiplier + bonusStats.globalDamageMultiplier),
            damageVsStatusTargets = baseStats.damageVsStatusTargets + bonusStats.damageVsStatusTargets,
            missingHealthDamage = baseStats.missingHealthDamage + bonusStats.missingHealthDamage,
            lowHealthPower = baseStats.lowHealthPower + bonusStats.lowHealthPower,

            maxHealth = Mathf.Max(1f, baseStats.maxHealth + bonusStats.maxHealth),
            healthRegen = baseStats.healthRegen + bonusStats.healthRegen,
            maxShield = Mathf.Max(0f, baseStats.maxShield + bonusStats.maxShield),
            shield = currentShield,
            armor = baseStats.armor + bonusStats.armor,
            resistance = Mathf.Clamp(baseStats.resistance + bonusStats.resistance, 0f, 0.95f),
            lifesteal = Mathf.Max(0f, baseStats.lifesteal + bonusStats.lifesteal),

            moveSpeed = Mathf.Max(0.1f, baseStats.moveSpeed + bonusStats.moveSpeed),
            dashSpeed = Mathf.Max(0.1f, baseStats.dashSpeed + bonusStats.dashSpeed),
            jumpCount = Mathf.Max(1, baseStats.jumpCount + bonusStats.jumpCount),
            airControl = Mathf.Clamp01(baseStats.airControl + bonusStats.airControl),
            globalAcceleration = Mathf.Max(0.1f, baseStats.globalAcceleration + bonusStats.globalAcceleration),

            cooldownReduction = Mathf.Clamp(baseStats.cooldownReduction + bonusStats.cooldownReduction, 0f, 0.8f),
            castSpeed = Mathf.Max(0.1f, baseStats.castSpeed + bonusStats.castSpeed),

            maxSkillResource = Mathf.Max(1f, baseStats.maxSkillResource + bonusStats.maxSkillResource),
            skillResource = currentSkillResource,
            resourceRegen = baseStats.resourceRegen + bonusStats.resourceRegen,

            procChance = Mathf.Clamp01(baseStats.procChance + bonusStats.procChance),
            procPower = Mathf.Max(0f, baseStats.procPower + bonusStats.procPower),
            procCount = Mathf.Max(1, baseStats.procCount + bonusStats.procCount),

            statusChance = Mathf.Clamp01(baseStats.statusChance + bonusStats.statusChance),
            statusDuration = Mathf.Max(0f, baseStats.statusDuration + bonusStats.statusDuration),

            killBonus = baseStats.killBonus + bonusStats.killBonus,
            killStreakBonus = baseStats.killStreakBonus + bonusStats.killStreakBonus,
            onHitTakenEffectPower = baseStats.onHitTakenEffectPower + bonusStats.onHitTakenEffectPower,
            statExchange = baseStats.statExchange + bonusStats.statExchange,

            attackRadius = Mathf.Max(0.1f, baseStats.attackRadius + bonusStats.attackRadius),
            skillRange = Mathf.Max(0.1f, baseStats.skillRange + bonusStats.skillRange),
            abilityHitboxSize = Mathf.Max(0.1f, baseStats.abilityHitboxSize + bonusStats.abilityHitboxSize),
            itemSlotLimit = Mathf.Max(1, baseStats.itemSlotLimit + bonusStats.itemSlotLimit)
        };

        return s;
    }

    public void ResetProperties()
    {
        HeroStats s = GetStats();
        currentHealth = s.maxHealth;
        currentShield = s.maxShield;
        currentSkillResource = s.maxSkillResource;
    }

    void RegenerateHealthAndResource()
    {
        HeroStats s = GetStats();
        if (currentHealth < s.maxHealth)
            currentHealth = Mathf.Min(s.maxHealth, currentHealth + s.healthRegen * Time.deltaTime);

        if (currentShield < s.maxShield)
            currentShield = Mathf.Min(s.maxShield, currentShield + s.healthRegen * 0.5f * Time.deltaTime);

        if (currentSkillResource < s.maxSkillResource)
            currentSkillResource = Mathf.Min(s.maxSkillResource, currentSkillResource + s.resourceRegen * Time.deltaTime);
    }


    public float GetCurrentHealthRatio()
    {
        float max = Mathf.Max(1f, GetStats().maxHealth);
        return Mathf.Clamp01(currentHealth / max);
    }

    public bool TrySpendSkillResource(float amount)
    {
        if (amount <= 0f)
            return true;

        if (currentSkillResource < amount)
            return false;

        currentSkillResource -= amount;
        return true;
    }

    public void Heal(float value)
    {
        if (value <= 0f)
            return;

        HeroStats s = GetStats();
        currentHealth = Mathf.Min(s.maxHealth, currentHealth + value);
        foreach (var bar in healthBarForeground)
            bar.value = currentHealth;
    }

    public void ChangeGun(int id)
    {
        Destroy(gunHolder.GetChild(0).gameObject);
        Instantiate(guns[id], gunHolder);

        Destroy(camGunHolder.GetChild(0).gameObject);
        Instantiate(guns[id], camGunHolder);

        ChangeGunProperties();
    }

    public void ChangeGunProperties()
    {
        ApplyStatsToGun(gunHolder.GetComponentInChildren<Gun>());
        ApplyStatsToGun(camGunHolder.GetComponentInChildren<Gun>());
    }

    void ApplyStatsToGun(Gun gun)
    {
        if (gun == null)
            return;

        HeroStats s = GetStats();
        gun.dmg = s.damage;
        gun.attackSpeedMultiplier = s.attackSpeed;
        gun.critChance = s.critChance;
        gun.critDmgMultiplier = s.critDamageMultiplier;
        gun.armorPenetration = s.armorPenetration;
        gun.globalDamageMultiplier = s.globalDamageMultiplier;
        gun.statusChance = s.statusChance;
        gun.statusDuration = s.statusDuration;
        gun.procChance = s.procChance;
        gun.procPower = s.procPower;
        gun.procCount = s.procCount;
        gun.radius = 5f * s.attackRadius;
        gun.skillRangeMultiplier = s.skillRange;
        gun.abilityHitboxSize = s.abilityHitboxSize;
        gun.cooldownReduction = s.cooldownReduction;
        gun.castSpeedMultiplier = s.castSpeed;
    }

    public void AddGems(int amount)
    {
        gems += amount;
        UpdateGemStatus();
        AddExperience(amount);
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        currentExperience += amount;

        while (currentExperience >= experienceForNextLevel)
        {
            currentExperience -= experienceForNextLevel;
            level++;

            experienceForNextLevel = Mathf.Max(experienceForNextLevel + 1, Mathf.RoundToInt(experienceForNextLevel * experienceGrowthMultiplier));

            OnLevelUp?.Invoke();
        }
        expirienceBar.maxValue = experienceForNextLevel;
        expirienceBar.DOValue(currentExperience, 1f);
    }

    void UpdateGemStatus()
    {
        if (gemStatus != null)
            gemStatus.text = gems.ToString();
    }

    void UpdateHealthBars()
    {
        float maxHealth = GetStats().maxHealth;
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

    public void GetDamage(float damage)
    {
        HeroStats s = GetStats();
        float reducedByArmor = damage * (100f / (100f + Mathf.Max(0f, s.armor)));
        float reducedDamage = reducedByArmor * (1f - s.resistance);

        if (currentShield > 0f)
        {
            float shieldDamage = Mathf.Min(currentShield, reducedDamage);
            currentShield -= shieldDamage;
            reducedDamage -= shieldDamage;
        }

        if (reducedDamage > 0f)
            currentHealth -= reducedDamage;

        healthChanged = true;
        foreach (var bar in healthBarForeground)
            bar.value = currentHealth;

        timeWithoutHealthChanhges = 0f;

        if (currentHealth <= 0)
            Die();
    }

    public void Die()
    {
        foreach (var effect in dieEffect)
            Instantiate(effect, transform.position + new Vector3(0, 1f, 0f), Quaternion.identity);
        ResetProperties();
        diePanel.showDiePanel();
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        gameObject.SetActive(false);
    }
}
