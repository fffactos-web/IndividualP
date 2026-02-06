using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Actions/Explosion On Kill")]
public class ExplosionOnKillAction : EffectAction
{
    public float radius = 3f;
    public float damage = 15f;
    public LayerMask targetMask = ~0;
    public bool includeTriggerColliders = false;
    public GameObject vfxPrefab;

    readonly Collider[] overlapBuffer = new Collider[64];

    public override bool CanExecute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        return ctx.targetWasKilled && radius > 0f && damage > 0f;
    }

    public override void Execute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        Vector3 center = ctx.hitPosition;

        if (vfxPrefab != null)
            Object.Instantiate(vfxPrefab, center, Quaternion.identity);

        QueryTriggerInteraction qti = includeTriggerColliders ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        int count = Physics.OverlapSphereNonAlloc(center, radius, overlapBuffer, targetMask, qti);

        for (int i = 0; i < count; i++)
        {
            var col = overlapBuffer[i];
            if (col == null) continue;

            var zombie = col.GetComponentInParent<Zombie_Properies>();
            if (zombie == null || zombie == target) continue;
            zombie.InternalApplyDamage(damage);
        }
    }
}
