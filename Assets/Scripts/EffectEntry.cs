using UnityEngine;

public enum EffectTriggerType
{
    OnHit,
    OnKill,
    OnDeath,
    OnDamaged,
    OnSpawn
}

[System.Serializable]
public class EffectEntry
{
    public ProcTrigger trigger = ProcTrigger.Any;

    [Range(0f, 1f)]
    public float procChance = 0.2f;

    public EffectAction action;
    public float cooldownSeconds = 0f;
}
