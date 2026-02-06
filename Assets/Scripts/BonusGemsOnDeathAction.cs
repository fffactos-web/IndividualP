using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Actions/Bonus Gems On Death")]
public class BonusGemsOnDeathAction : EffectAction
{
    public Vector2Int flatBonusRange = new Vector2Int(1, 3);
    public float dropMultiplier = 1f;

    public override bool CanExecute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        return ctx.targetWasKilled && source is IGemCollector;
    }

    public override void Execute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        int min = Mathf.Min(flatBonusRange.x, flatBonusRange.y);
        int max = Mathf.Max(flatBonusRange.x, flatBonusRange.y);
        int flat = Random.Range(min, max + 1);
        int total = Mathf.Max(0, Mathf.RoundToInt(flat * Mathf.Max(0f, dropMultiplier)));

        if (total <= 0) return;
        ((IGemCollector)source).AddGems(total);
    }
}
