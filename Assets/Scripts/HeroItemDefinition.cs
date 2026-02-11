using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Hero Item", menuName = "Game/Hero Item", order = 10)]
public class HeroItemDefinition : ScriptableObject
{
    virtual public void OnEquip() { }

    [Header("Presentation")]
    [SerializeField] string itemName;
    [SerializeField, TextArea(2, 5)] string description;
    [SerializeField] Sprite icon;
    [SerializeField] ItemRarity rarity = ItemRarity.Common;
    [SerializeField] float cost;

    [Header("Stat bonuses")]
    [SerializeField] List<HeroStatModifier> statModifiers = new List<HeroStatModifier>();

    public string ItemName => itemName;
    public string Description => description;
    public float Cost => cost;
    public Sprite Icon => icon;
    public ItemRarity Rarity => rarity;
    public IReadOnlyList<HeroStatModifier> StatModifiers => statModifiers;
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Mythic,
    Legendary
}

[Serializable]
public struct HeroStatModifier
{
    public HeroStatType stat;
    public float value;
}


public enum HeroStatType
{
    Damage,
    AttackSpeed,
    CritChance,
    CritDamageMultiplier,
    ArmorPenetration,
    GlobalDamageMultiplier,
    GlobalAttackSpeed,
    DamageVsStatusTargets,
    MissingHealthDamage,
    LowHealthPower,

    MaxHealth,
    HealthRegen,
    Shield,
    MaxShield,
    Armor,
    Resistance,
    Lifesteal,

    MoveSpeed,
    DashSpeed,
    JumpCount,
    AirControl,
    GlobalAcceleration,

    CooldownReduction,
    CastSpeed,

    SkillResource,
    MaxSkillResource,
    ResourceRegen,

    ProcChance,
    ProcPower,
    ProcCount,

    StatusChance,
    StatusDuration,

    KillBonus,
    KillStreakBonus,
    OnHitTakenEffectPower,
    StatExchange,
    Luck,

    AttackRadius,
    SkillRange,
    AbilityHitboxSize,
    ItemSlotLimit
}
