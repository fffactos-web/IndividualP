using UnityEngine;

public abstract class EffectAction : ScriptableObject
{
    [Header("Diagnostics")]
    [Tooltip("Stable ID for analytics/save migration/debug traces.")]
    public string effectId = "effect.unknown";

    [Tooltip("Readable name for logs and inspector debugging.")]
    public string debugName = "Unnamed Effect";

    /// <summary>
    /// Guard method that decides if this action can run for the current proc.
    /// Override it in custom actions to block execution based on context.
    /// </summary>
    public virtual bool CanExecute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        return true;
    }

    /// <summary>
    /// Executes side effects for this action.
    ///
    /// Custom effects should prefer reading data from:
    /// - ctx.damageDone / ctx.isCrit / ctx.hitLayer
    /// - ctx.targetWasKilled / ctx.hitPosition
    /// - source (attacker/owner) and target (victim)
    ///
    /// Allowed side effects:
    /// - apply damage/status to target or nearby valid entities
    /// - spawn pooled VFX/SFX
    /// - grant currency/rewards to source via public APIs/interfaces
    /// - enqueue additional procs through shared managers
    ///
    /// Avoid in custom actions:
    /// - direct scene lookups by name
    /// - hard dependencies on specific MonoBehaviours when a public API/interface exists
    /// - mutating unrelated global state
    /// </summary>
    public abstract void Execute(Character_Properties source, Zombie_Properies target, ProcContext ctx);
}

[CreateAssetMenu(menuName = "Effects/Actions/DealExtraDamage")]
public class DealExtraDamageAction : EffectAction
{
    public float extraDamage = 5f;

    public override bool CanExecute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        return target != null;
    }

    public override void Execute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        target.InternalApplyDamage(extraDamage);
    }
}

[CreateAssetMenu(menuName = "Effects/Actions/SpawnCoins")]
public class SpawnCoinsAction : EffectAction
{
    public int count = 6;
    public float force = 3f;

    public override bool CanExecute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        return target != null && PoolManager.I != null && PoolManager.I.gemPool != null;
    }

    public override void Execute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        var pos = target.transform.position + Vector3.up * 0.5f;
        for (int i = 0; i < count; i++)
        {
            var go = PoolManager.I.gemPool.Spawn(pos, Quaternion.identity);
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 f = (Vector3.up + Random.onUnitSphere * 0.5f).normalized * (force * Random.Range(0.7f, 1.3f));
                rb.AddForce(f, ForceMode.Impulse);
            }
        }
    }
}
