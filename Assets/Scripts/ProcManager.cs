using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class ProcManager : MonoBehaviour
{
    public static ProcManager Instance { get; private set; }

    struct ProcEvent
    {
        public Character_Properties source;
        public Zombie_Properies target;
        public EffectData effects;
        public IReadOnlyList<EffectEntry> runtimeEntries;
        public int entriesKey;
        public ProcContext ctx;
    }

    List<ProcEvent> queue = new List<ProcEvent>(256);

    // targetInstanceId -> entriesKey -> lastTimeByEntryIndex
    readonly Dictionary<int, Dictionary<int, float[]>> lastTriggerTime = new Dictionary<int, Dictionary<int, float[]>>();

    [Tooltip("Макс. procs обработок за кадр")]
    public int maxProcessPerFrame = 128;

        queue.Add(new ProcEvent { source = source, target = target, effects = effects, ctx = ctx });
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Update()
    {
        if (queue.Count == 0) return;
        int toProcess = Mathf.Min(maxProcessPerFrame, queue.Count);
        for (int i = 0; i < toProcess; i++)
        {
            var ev = queue[i];
            ProcessEvent(ref ev);
        }

        if (toProcess > 0) queue.RemoveRange(0, toProcess);
    }

    void ProcessEvent(ref ProcEvent ev)
    {
        if (ev.target == null) return;

        IReadOnlyList<EffectEntry> entries = GetEntries(ev);
        if (entries == null || entries.Count == 0) return;

        float[] timers = GetTimers(ev.target.GetInstanceID(), ev.entriesKey, entries.Count);
        ProcessEntries(entries, timers, ref ev);
    }

    IReadOnlyList<EffectEntry> GetEntries(in ProcEvent ev)
    {
        if (ev.effects != null && ev.effects.entries != null)
            return ev.effects.entries;

        return ev.runtimeEntries;
    }

    float[] GetTimers(int targetId, int entriesKey, int requiredLength)
    {
        if (!lastTriggerTime.TryGetValue(targetId, out var timersByEntries))
        {
            timersByEntries = new Dictionary<int, float[]>();
            lastTriggerTime[targetId] = timersByEntries;
        }

        if (!timersByEntries.TryGetValue(entriesKey, out var timers))
        {
            timers = CreateTimerArray(requiredLength);
            timersByEntries[entriesKey] = timers;
            return timers;
        }

        if (timers.Length < requiredLength)
        {
            var newArr = CreateTimerArray(requiredLength);
            for (int i = 0; i < timers.Length; i++)
                newArr[i] = timers[i];

            timers = newArr;
            timersByEntries[entriesKey] = timers;
        }

        return timers;
    }

    static float[] CreateTimerArray(int length)
    {
        var arr = new float[length];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = -9999f;

        return arr;
    }

    void ProcessEntries(IReadOnlyList<EffectEntry> entries, float[] timers, ref ProcEvent ev)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || entry.action == null) continue;
            if (Random.value > entry.procChance) continue;

            if (entry.cooldownSeconds > 0f)
            {
                if (Time.time - timers[i] < entry.cooldownSeconds) continue;
                timers[i] = Time.time;
            }

            entry.action.Execute(ev.source, ev.target, ev.ctx);
        }
    }

    public void QueueProc(Zombie_Properies target, EffectData effects, ProcContext ctx)
    {
        if (effects == null || effects.entries == null || target == null) return;
        if (queue.Count > 20000) return;

        queue.Add(new ProcEvent
        {
            target = target,
            effects = effects,
            entriesKey = effects.GetInstanceID(),
            ctx = ctx
        });
    }

    public void QueueProc(Zombie_Properies target, IReadOnlyList<EffectEntry> effects, ProcContext ctx)
    {
        if (effects == null || effects.Count == 0 || target == null) return;
        if (queue.Count > 20000) return;

        queue.Add(new ProcEvent
        {
            target = target,
            runtimeEntries = effects,
            entriesKey = RuntimeHelpers.GetHashCode(effects),
            ctx = ctx
        });
    }

    public void QueueProc(Zombie_Properies target, List<EffectEntry> effects, ProcContext ctx)
    {
        QueueProc(target, (IReadOnlyList<EffectEntry>)effects, ctx);
    }
}
