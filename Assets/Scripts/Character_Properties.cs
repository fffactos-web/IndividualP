using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
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
        public float globalAttackSpeedMultiplier = 1f;

        [Header("Defense")]
        public float maxHealth = 100f;
        public float healthRegen = 0f;
        public float shield = 0f;
        public float maxShield = 0f;
        public float armor = 0f;
        [Range(0f, 0.95f)] public float resistance = 0f;

        [Header("Movement")]
        public float moveSpeed = 1f;
        public float runSpeed = 1f;
        public float maxStamina = 100f;
        public int jumpCount = 1;
        [Range(0f, 1f)] public float airControl = 0.35f;
        public float globalAcceleration = 1f;

        [Header("Cooldowns / cast")]
        [Range(0f, 1f)] public float cooldownReduction = 0f;

        [Header("Procs")]
        [Range(0f, 1f)] public float procChance = 0f;
        public float procPower = 1f;

        [Header("Statuses")]
        [Range(0f, 1f)] public float statusChanceBonus = 0f;
        public float statusDurationBonus = 1f;

        public float luck = 0f;

        [Header("AoE / ranges")]
        public float attackRadius = 1f;

        [Header("Additional")]
        public float gold = 1f;
        public float exp = 1f;

    }

    [Header("References")]
    [SerializeField] SO_MetaReferences metaReferences;
    [SerializeField] GameObject[] dieEffect;
    [SerializeField] GameObject[] guns;
    [SerializeField] DiePanel diePanel;
    [SerializeField] UnityEngine.UI.Slider[] healthBars;
    [SerializeField] UnityEngine.UI.Slider[] healthBarForeground;
    [SerializeField] UnityEngine.UI.Slider expirienceBar;
     Transform gunHolder;
    [SerializeField] Transform camGunHolder;
    [SerializeField] Shop shop;
    [SerializeField] HeroStats[] statsPresets;

    [Header("Stats")]
    public HeroStats baseStats = new HeroStats();
    public Character_StatusBar sBar;

    [Header("Progression")]
    [SerializeField] int startExperienceForNextLevel = 20;
    [SerializeField] float experienceGrowthMultiplier = 1.25f;

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
    public int currentExperience { get; private set; }
    public int experienceForNextLevel { get; private set; }

    public event Action OnLevelUp;
    public event Action OnGetDamage;

    public bool died = true;
    private IEnumerator Start()
    {
        while (gunHolder == null || camGunHolder == null)
        {
            if (gunHolder == null)
            {
                var obj = GameObject.FindGameObjectWithTag("Gunn");
                if (obj != null)
                    gunHolder = obj.transform;
            }

            if (camGunHolder == null)
            {
                var objo = GameObject.FindGameObjectWithTag("Camera Gun");
                if (objo != null)
                    camGunHolder = objo.transform;
            }

            yield return null;
        }

        Instantiate(guns[metaReferences.characterID], gunHolder);
        Instantiate(guns[metaReferences.characterID], camGunHolder);
        camGunHolder.gameObject.SetActive(false);


        baseStats = statsPresets[metaReferences.characterID];

        ChangeGunProperties();
    }

    private void Awake()
    {
        baseStats = statsPresets[metaReferences.characterID];

        ResetProperties();

        diePanel.gameObject.SetActive(false);
        gemStatus = GameObject.FindGameObjectWithTag("Gem Status").GetComponent<TextMeshProUGUI>();
        experienceForNextLevel = Mathf.Max(1, startExperienceForNextLevel);
        expirienceBar.maxValue = experienceForNextLevel;
        expirienceBar.value = currentExperience;

        UpdateHealthBars();
        UpdateGemStatus();

        OnLevelUp = () => LevelUp();
    }

    void LevelUp()
    {
        shop.gameObject.SetActive(true);
        shop.RefreshItems();
        Time.timeScale = 0f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
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
            damage = baseStats.damage,
            attackSpeed = baseStats.attackSpeed,
            globalAttackSpeedMultiplier = baseStats.globalAttackSpeedMultiplier,
            critChance = baseStats.critChance,
            critDamageMultiplier = baseStats.critDamageMultiplier,
            armorPenetration = baseStats.armorPenetration,
            globalDamageMultiplier = baseStats.globalDamageMultiplier,

            maxHealth = baseStats.maxHealth,
            healthRegen = baseStats.healthRegen,
            maxShield = baseStats.maxShield,
            shield = currentShield,
            armor = baseStats.armor + baseStats.armor,
            resistance = baseStats.resistance,

            moveSpeed = baseStats.moveSpeed,
            runSpeed = baseStats.runSpeed,
            maxStamina = baseStats.maxStamina,
            jumpCount = baseStats.jumpCount,
            airControl = baseStats.airControl,
            globalAcceleration = baseStats.globalAcceleration,

            cooldownReduction = baseStats.cooldownReduction,

            procChance = baseStats.procChance,
            procPower = baseStats.procPower,

            luck = baseStats.luck + baseStats.luck,

            attackRadius = baseStats.attackRadius,
            
            gold = baseStats.gold,
            exp = baseStats.exp
        };

        return s;
    }

    public void ResetProperties()
    {
        HeroStats s = GetStats();
        GetComponent<Movement>().ResetProperties();
        currentHealth = s.maxHealth;
        currentShield = s.maxShield;
    }

    void RegenerateHealthAndResource()
    {
        HeroStats s = GetStats();
        if (currentHealth < s.maxHealth)
        {
            currentHealth = Mathf.Min(s.maxHealth, currentHealth + s.healthRegen * Time.deltaTime);
            UpdateHealthBars();
        }

        if (currentShield < s.maxShield)
            currentShield = Mathf.Min(s.maxShield, currentShield + s.healthRegen * 0.5f * Time.deltaTime);
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
        if (id < 0 || id >= guns.Length)
            return;

        if (gunHolder != null && gunHolder.childCount > 0)
            Destroy(gunHolder.GetChild(0).gameObject);

        Instantiate(guns[id], gunHolder);

        if (camGunHolder != null && camGunHolder.childCount > 0)
            Destroy(camGunHolder.GetChild(0).gameObject);

        Instantiate(guns[id], camGunHolder);

        ChangeGunProperties();
    }

    public void ChangeGunProperties()
    {
        ApplyStatsToGun(gunHolder.GetComponentInChildren<Gun>());
        ApplyStatsToGun(camGunHolder.GetComponentInChildren<Gun>());
    }

    public bool ApplyItem(HeroItemDefinition item)
    {
        if (item == null)
            return false;

        item.OnEquip();

        foreach (var modifier in item.StatModifiers)
            ApplyModifier(modifier);

        GetComponent<Movement>().ResetProperties();
        ChangeGunProperties();
        UpdateHealthBars();
        return true;
    }

    public bool RemoveItem(HeroItemDefinition item)
    {
        if (item == null)
            return false;

        foreach (var modifier in item.StatModifiers)
            ApplyModifier(new HeroStatModifier { stat = modifier.stat, value = -modifier.value });

        ChangeGunProperties();
        UpdateHealthBars();
        return true;
    }

    public void ApplyModifier(HeroStatModifier modifier)
    {
        switch (modifier.stat)
        {
            case HeroStatType.Damage:
                baseStats.damage += (baseStats.damage/100)*modifier.value;
                break;
            case HeroStatType.AttackSpeed:
                baseStats.attackSpeed += (baseStats.attackSpeed/100) * modifier.value;
                break;
            case HeroStatType.CritChance:
                baseStats.critChance += modifier.value;
                break;
            case HeroStatType.CritDamageMultiplier:
                baseStats.critDamageMultiplier += (baseStats.critDamageMultiplier/100)*modifier.value;
                break;
            case HeroStatType.ArmorPenetration:
                baseStats.armorPenetration += (baseStats.armorPenetration / 100) *modifier.value;
                break;
            case HeroStatType.GlobalAttackSpeed:
                baseStats.globalAttackSpeedMultiplier += (baseStats.globalAttackSpeedMultiplier / 100) *modifier.value;
                break;
            case HeroStatType.GlobalDamageMultiplier:
                baseStats.globalDamageMultiplier += (baseStats.globalDamageMultiplier / 100) *modifier.value;
                break;

            case HeroStatType.MaxHealth:
                baseStats.maxHealth += modifier.value;
                break;
            case HeroStatType.HealthRegen:
                baseStats.healthRegen += modifier.value;
                break;
            case HeroStatType.Shield:
                baseStats.shield += modifier.value;
                currentShield = Mathf.Max(0f, currentShield + modifier.value);
                break;
            case HeroStatType.MaxShield:
                baseStats.maxShield += modifier.value;
                break;
            case HeroStatType.Armor:
                baseStats.armor += modifier.value;
                break;
            case HeroStatType.Resistance:
                baseStats.resistance += (baseStats.resistance / 100) *modifier.value;
                break;

            case HeroStatType.MoveSpeed:
                baseStats.moveSpeed += (baseStats.moveSpeed / 100) *modifier.value;
                break;
            case HeroStatType.MaxStamina:
                baseStats.maxStamina += modifier.value;
                break;
            case HeroStatType.DashSpeed:
                baseStats.runSpeed += (baseStats.runSpeed / 100) *modifier.value;
                break;
            case HeroStatType.JumpCount:
                baseStats.jumpCount += Mathf.RoundToInt(modifier.value);
                break;
            case HeroStatType.AirControl:
                baseStats.airControl += (baseStats.airControl / 100) *modifier.value;
                break;
            case HeroStatType.GlobalAcceleration:
                baseStats.globalAcceleration += (baseStats.globalAcceleration / 100) *modifier.value;
                break;

            case HeroStatType.ProcChance:
                baseStats.procChance += (baseStats.procChance / 100) *modifier.value;
                break;
            case HeroStatType.ProcPower:
                baseStats.procPower += (baseStats.procPower / 100) *modifier.value;
                break;

            case HeroStatType.Luck:
                baseStats.luck += (baseStats.luck / 100) *modifier.value;
                break;

            case HeroStatType.AttackRadius:
                baseStats.attackRadius += (baseStats.attackRadius / 100) *modifier.value;
                break;

            case HeroStatType.Gold:
                baseStats.gold += (baseStats.gold / 100) * modifier.value;
                break;
            case HeroStatType.Expirience:
                baseStats.exp += (baseStats.exp / 100) * modifier.value;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (RecentRewardsFeedUI.I != null)
            RecentRewardsFeedUI.I.ShowModifier(modifier);
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
        gun.globalAttackSpeed = s.globalAttackSpeedMultiplier;
        gun.procChance = s.procChance;
        gun.procPower = s.procPower;
        gun.radius = 5f * s.attackRadius;
        gun.cooldownReduction = s.cooldownReduction;
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

    public void AddGems(int amount)
    {
        gems += amount;
        UpdateGemStatus();
        AddExperience(amount);
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
        OnGetDamage?.Invoke();
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
        Character_StatusBar.I.OnDie.Invoke(this);
        if (died)
        {
            foreach (var effect in dieEffect)
                Instantiate(effect, transform.position + new Vector3(0, 1f, 0f), Quaternion.identity);
            ResetProperties();
            diePanel.showDiePanel();
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            gameObject.SetActive(false);
            Time.timeScale = 0f;
        }
    }
}
