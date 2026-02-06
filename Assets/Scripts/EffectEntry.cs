using UnityEngine;

[System.Serializable]
public class EffectEntry
{
    [Tooltip("When this effect is allowed to trigger")]
    public ProcTrigger trigger = ProcTrigger.Any;

    [Range(0f, 1f)]
    public float procChance = 0.2f;

    public EffectAction action;

    [Min(0f)]
    public float cooldownSeconds = 0f;
}
