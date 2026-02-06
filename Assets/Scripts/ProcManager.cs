using System.Collections.Generic;
using UnityEngine;

public class ProcManager : MonoBehaviour
{
    public static ProcManager Instance { get; private set; }

    readonly struct CooldownKey
    {
        public readonly int targetId;
        public readonly int actionId;
        public readonly ProcTrigger trigger;

        public CooldownKey(int targetId, int actionId, ProcTrigger trigger)
        {
            this.targetId = targetId;
            this.actionId = actionId;
            this.trigger = trigger;
        }
    }

    readonly Dictionary<CooldownKey, float> lastTriggerTimeByKey = new Dictionary<CooldownKey, float>(1024);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void QueueProc(Character_Properties source, Zombie_Properies target, EffectData effects, ProcContext ctx)
    {
        if (effects == null || effects.entries == null)
            return;

        ProcessEntries(source, target, effects.entries, ctx);
    }

    public void QueueProc(Character_Properties source, Zombie_Properies target, IReadOnlyList<EffectEntry> runtimeEntries, int entriesKey, ProcContext ctx)
    {
        if (runtimeEntries == null)
            return;

        ProcessEntries(source, target, runtimeEntries, ctx);
    }

    void ProcessEntries(Character_Properties source, Zombie_Properies target, IReadOnlyList<EffectEntry> entries, ProcContext ctx)
    {
        if (target == null || entries == null || entries.Count == 0)
            return;

        int targetId = target.GetInstanceID();

        for (int i = 0; i < entries.Count; i++)
        {
            EffectEntry entry = entries[i];
            if (entry == null || entry.action == null)
                continue;

            if (entry.trigger != ProcTrigger.Any && entry.trigger != ctx.trigger)
                continue;

            if (entry.procChance < 1f && Random.value > entry.procChance)
                continue;

            if (entry.cooldownSeconds > 0f)
            {
                CooldownKey cooldownKey = new CooldownKey(targetId, entry.action.GetInstanceID(), ctx.trigger);
                if (lastTriggerTimeByKey.TryGetValue(cooldownKey, out float lastTriggerTime) &&
                    Time.time - lastTriggerTime < entry.cooldownSeconds)
                {
                    continue;
                }

                lastTriggerTimeByKey[cooldownKey] = Time.time;
            }

            if (!entry.action.CanExecute(source, target, ctx))
                continue;

            entry.action.Execute(source, target, ctx);
        }
    }
}
