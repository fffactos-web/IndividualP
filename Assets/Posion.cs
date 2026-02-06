using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Actions/Poison")]
public class Poison : EffectAction
{
    [Tooltip("Damage per second")]
    public float damagePerSecond = 5f;

    [Tooltip("Duration in seconds")]
    public float duration = 3f;

    [Tooltip("Tick interval in seconds")]
    public float tickInterval = 1f;

    public enum StackingMode { Refresh, Stack, Ignore }
    public StackingMode stacking = StackingMode.Refresh;

    public override bool CanExecute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        return target != null;
    }

    public override void Execute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        if (target == null) return;
        int effectKey = GetInstanceID();
        target.ApplyDot(effectKey, damagePerSecond, duration, Mathf.Max(0.01f, tickInterval), source, stacking);
    }

    //PoisonOnHitAction.StackingMode ToNewStacking(StackingMode mode)
    //{
    //    switch (mode)
    //    {
    //        case StackingMode.Stack: return PoisonOnHitAction.StackingMode.Stack;
    //        case StackingMode.Ignore: return PoisonOnHitAction.StackingMode.Ignore;
    //        default: return PoisonOnHitAction.StackingMode.Refresh;
    //    }
    //}
}
