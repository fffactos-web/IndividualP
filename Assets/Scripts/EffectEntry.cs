using UnityEngine;

[System.Serializable]
public class EffectEntry
{
    public ProcTrigger trigger = ProcTrigger.Any;

    [Range(0f, 1f)]
    public float procChance = 0.2f;

    public EffectAction action;
    public float cooldownSeconds = 0f;
}
