using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Actions/Poison")]
public class Poison : EffectAction
{
    [Tooltip("”рон в секунду")]
    public float damagePerSecond = 5f;

    [Tooltip("ƒлительность в секундах")]
    public float duration = 3f;

    [Tooltip("»нтервал тика (сек). 1 = урон каждую секунду")]
    public float tickInterval = 1f;

    public enum StackingMode { Refresh, Stack, Ignore }
    [Tooltip("Refresh Ч перезаписывает таймер (обновл€ет длительность). Stack Ч добавл€ет новый отдельный DoT. Ignore Ч не примен€ет, если уже висит.")]
    public StackingMode stacking = StackingMode.Refresh;

    public override void Execute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        if (target == null) return;
        // ключ эффекта Ч используем ID ассета (GetInstanceID) дл€ идентификации одного типа эффекта
        int effectKey = this.GetInstanceID();
        target.ApplyDot(effectKey, damagePerSecond, duration, Mathf.Max(0.01f, tickInterval), source, stacking);
    }
}
