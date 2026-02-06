using UnityEngine;

public abstract class EffectAction : ScriptableObject
{
    // Выполняется синхронно в главном потоке (может вызывать PoolManager и др.)
    public abstract void Execute(Character_Properties source, Zombie_Properies target, ProcContext ctx);
}

[CreateAssetMenu(menuName = "Effects/Actions/DealExtraDamage")]
public class DealExtraDamageAction : EffectAction
{
    public float extraDamage = 5f;
    public override void Execute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        if (target == null) return;
        // Используем внутренний метод без лишних аллокаций
        target.InternalApplyDamage(extraDamage);
    }
}

[CreateAssetMenu(menuName = "Effects/Actions/SpawnCoins")]
public class SpawnCoinsAction : EffectAction
{
    public GameObject coinPrefab;
    public int count = 6;
    public float force = 3f;

    public override void Execute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        if (coinPrefab == null || target == null) return;
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
