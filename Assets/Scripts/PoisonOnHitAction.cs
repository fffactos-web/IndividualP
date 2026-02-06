using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Actions/Poison On Hit")]
public class PoisonOnHitAction : EffectAction
{
    public enum StackingMode { Refresh, Stack, Ignore }

    [Range(0f, 1f)]
    public float poisonChance = 0.35f;
    public float damagePerSecond = 5f;
    public float duration = 3f;
    public float tickInterval = 1f;
    public StackingMode stacking = StackingMode.Refresh;

    public override bool CanExecute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        return target != null && damagePerSecond > 0f && duration > 0f && Random.value <= poisonChance;
    }

    public override void Execute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        int effectKey = GetInstanceID();
        target.ApplyDot(effectKey, damagePerSecond, duration, Mathf.Max(0.01f, tickInterval), source, ToLegacyStacking(stacking));
    }

    Poison.StackingMode ToLegacyStacking(StackingMode mode)
    {
        switch (mode)
        {
            case StackingMode.Stack: return Poison.StackingMode.Stack;
            case StackingMode.Ignore: return Poison.StackingMode.Ignore;
            default: return Poison.StackingMode.Refresh;
        }
    }
}
