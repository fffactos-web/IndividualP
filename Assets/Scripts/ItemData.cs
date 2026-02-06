using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Универсальная структура модификатора (стата) для предмета.
/// </summary>
[Serializable]
public struct StatModifier
{
    public ItemStat stat;
    public float value;
}

/// <summary>
/// Перечисление возможных статов — ограничено теми, что реально встречаются в Character_Properties.
/// Добавляй новые элементы по мере расширения Character_Properties.
/// </summary>
public enum ItemStat
{
    Damage,
    AttackSpeed,
    CritChance,
    CritDamage,
    ArmorPenetration,

    MaxHealth,
    MaxHealthBonus,
    HealthRegen,
    Shield,

    Armor,
    Resistance,

    LifeSteal,

    MoveSpeed,
    DashSpeed,
    ExtraJumps,

    ProcChance,
    ProcPower,
    ProcCount,

    StatusChance,
    StatusDuration,

    DamageToAffectedTargets,
    OnKillExplosionDamage,
    KillStreakAttackSpeed,

    MissingHealthDamage,
    LowHealthBonus,

    AttackRadius,
    AbilityRange,
    HitboxSize,

    GlobalDamageMultiplier,
    GlobalSpeedMultiplier
}

/// <summary>
/// ScriptableObject предмета: содержит набор стат-модификаторов и эффектов.
/// </summary>
[CreateAssetMenu(menuName = "Game/Item", fileName = "NewItem")]
public class ItemData : ScriptableObject
{
    public string itemName = "NewItem";
    [TextArea(2, 4)] public string description;
    public Sprite icon;
    public int shopCost;

    [Header("Stats")]
    public StatModifier[] statModifiers;

    [Header("Effects (optional)")]
    public EffectEntry[] effects;

    [Header("Drop")]
    [Range(0f, 1f)] public float baseDropChance = 0.1f;

    // --- APPLY / REMOVE ---

    /// <summary>
    /// Применить предмет к персонажу: добавляет значения в поля Character_Properties.
    /// </summary>
    public void ApplyTo(Character_Properties character)
    {
        if (character == null) return;

        foreach (var sm in statModifiers)
            ApplyModifier(character, sm, +1f);

        // эффекты (если нужны) можно добавить в список activeEffects персонажа
        if (effects != null && effects.Length > 0)
        {
            foreach (var e in effects)
            {
                // Если у Character_Properties есть activeEffects (как раньше), добавляем туда.
                var listField = character.GetType().GetField("activeEffects", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (listField != null)
                {
                    var listObj = listField.GetValue(character);
                    if (listObj is System.Collections.IList list)
                        list.Add(e);
                }
            }
        }

        // Пересчитать статы (если метод есть)
        TryCallRecalc(character);
    }

    /// <summary>
    /// Убрать предмет (например, снять/удалить) — обратная операция.
    /// </summary>
    public void RemoveFrom(Character_Properties character)
    {
        if (character == null) return;

        foreach (var sm in statModifiers)
            ApplyModifier(character, sm, -1f);

        if (effects != null && effects.Length > 0)
        {
            var listField = character.GetType().GetField("activeEffects", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (listField != null)
            {
                var listObj = listField.GetValue(character) as System.Collections.IList;
                if (listObj != null)
                {
                    foreach (var e in effects)
                    {
                        // простой remove: будет работать если Equals у EffectEntry корректен (по полям)
                        listObj.Remove(e);
                    }
                }
            }
        }

        TryCallRecalc(character);
    }

    void TryCallRecalc(Character_Properties character)
    {
        var m = character.GetType().GetMethod("RecalculateStats", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m != null) m.Invoke(character, null);
    }

    /// <summary>
    /// Применяет один модификатор: сначала пробуем найти поле с суффиксом "Bonus" (например damageBonus),
    /// если не найден — пробуем применить в базовое поле (например damage).
    /// Это даёт совместимость с разными версиями Character_Properties.
    /// </summary>
    void ApplyModifier(Character_Properties c, StatModifier modifier, float sign = +1f)
    {
        if (c == null) return;
        float delta = modifier.value * sign;

        // маппинг Stat -> возможные имена полей в Character_Properties (порядок важен)
        string[] candidates = GetCandidateFieldNames(modifier.stat);

        bool applied = false;
        var t = c.GetType();
        foreach (var name in candidates)
        {
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (f != null && (f.FieldType == typeof(float) || f.FieldType == typeof(int)))
            {
                if (f.FieldType == typeof(float))
                {
                    float cur = (float)f.GetValue(c);
                    f.SetValue(c, cur + delta);
                }
                else // int
                {
                    int cur = (int)f.GetValue(c);
                    f.SetValue(c, cur + Mathf.RoundToInt(delta));
                }
                applied = true;
                break;
            }

            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (p != null && (p.PropertyType == typeof(float) || p.PropertyType == typeof(int)))
            {
                if (p.PropertyType == typeof(float))
                {
                    float cur = (float)p.GetValue(c);
                    p.SetValue(c, cur + delta, null);
                }
                else
                {
                    int cur = (int)p.GetValue(c);
                    p.SetValue(c, cur + Mathf.RoundToInt(delta), null);
                }
                applied = true;
                break;
            }
        }

        if (!applied)
        {
            Debug.LogWarning($"ItemData.ApplyModifier: couldn't apply stat {modifier.stat} to character (no matching field).");
        }
    }

    string[] GetCandidateFieldNames(ItemStat stat)
    {
        // Возвращаем варианты имён полей в порядке приоритета
        switch (stat)
        {
            case ItemStat.Damage: return new[] { "damageBonus", "damage" };
            case ItemStat.AttackSpeed: return new[] { "attackSpeedBonus", "attackSpeed" };
            case ItemStat.CritChance: return new[] { "critChanceBonus", "critChance" };
            case ItemStat.CritDamage: return new[] { "critDamageBonus", "critDamage" };
            case ItemStat.ArmorPenetration: return new[] { "armorPenetrationBonus", "armorPenetration" };

            case ItemStat.MaxHealth: return new[] { "maxHealth", "baseMaxHealth" };
            case ItemStat.MaxHealthBonus: return new[] { "maxHealthBonus", "maxHealth" };
            case ItemStat.HealthRegen: return new[] { "healthRegenBonus", "healthRegen" };
            case ItemStat.Shield: return new[] { "shield" };

            case ItemStat.Armor: return new[] { "armorBonus", "armor" };
            case ItemStat.Resistance: return new[] { "resistanceBonus", "resistance" };

            case ItemStat.LifeSteal: return new[] { "lifeStealBonus", "lifeSteal" };

            case ItemStat.MoveSpeed: return new[] { "moveSpeedBonus", "moveSpeed" };
            case ItemStat.DashSpeed: return new[] { "dashSpeedBonus", "dashSpeed" };
            case ItemStat.ExtraJumps: return new[] { "extraJumpsBonus", "extraJumps" };

            case ItemStat.ProcChance: return new[] { "procChanceBonus", "procChance" };
            case ItemStat.ProcPower: return new[] { "procPowerBonus", "procPower" };
            case ItemStat.ProcCount: return new[] { "procCountBonus", "procCount" };

            case ItemStat.StatusChance: return new[] { "statusChance", "statusChanceBonus" };
            case ItemStat.StatusDuration: return new[] { "statusDuration", "statusDurationBonus" };

            case ItemStat.DamageToAffectedTargets: return new[] { "damageToAffectedTargets" };
            case ItemStat.OnKillExplosionDamage: return new[] { "onKillExplosionDamage" };
            case ItemStat.KillStreakAttackSpeed: return new[] { "killStreakAttackSpeed" };

            case ItemStat.MissingHealthDamage: return new[] { "missingHealthDamage" };
            case ItemStat.LowHealthBonus: return new[] { "lowHealthBonus" };

            case ItemStat.AttackRadius: return new[] { "attackRadiusBonus", "attackRadius" };
            case ItemStat.AbilityRange: return new[] { "abilityRange" };
            case ItemStat.HitboxSize: return new[] { "hitboxSize" };

            case ItemStat.GlobalDamageMultiplier: return new[] { "globalDamageMultiplier" };
            case ItemStat.GlobalSpeedMultiplier: return new[] { "globalSpeedMultiplier" };

            default:
                return new string[0];
        }
    }
}
