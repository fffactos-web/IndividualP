using UnityEngine;

[System.Serializable]
public class EffectEntry
{
    [Range(0f, 1f)]
    public float procChance = 0.2f; // шанс 0..1
    public EffectAction action; // ScriptableObject действие
    public float cooldownSeconds = 0f; // 0 = без кулдауна
}
