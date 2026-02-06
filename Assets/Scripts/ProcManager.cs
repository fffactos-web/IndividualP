using System.Collections.Generic;
using UnityEngine;

public class ProcManager : MonoBehaviour
{
    public static ProcManager Instance { get; private set; }

    struct ProcEvent
    {
        public Character_Properties source;
        public Zombie_Properies target;
        public IReadOnlyList<EffectEntry> entries;
        public ProcContext ctx;
    }

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

    readonly struct ProcEventKey
    {
        public readonly int frame;
        public readonly int targetId;
        public readonly int sourceId;
        public readonly int entriesKey;
        public readonly ProcTrigger trigger;

        public ProcEventKey(int frame, int targetId, int sourceId, int entriesKey, ProcTrigger trigger)
        {
            this.frame = frame;
            this.targetId = targetId;
            this.sourceId = sourceId;
            this.entriesKey = entriesKey;
            this.trigger = trigger;
        }
    }

    readonly List<ProcEvent> queue = new List<ProcEvent>(256);
    readonly Dictionary<CooldownKey, float> lastTriggerTimeByKey = new Dictionary<CooldownKey, float>(1024);

    readonly HashSet<ProcEventKey> queuedThisFrame = new HashSet<ProcEventKey>();
    int guardedFrame = -1;

    [Tooltip("Max amount of procs processed per frame")]
    public int maxProcessPerFrame = 128;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Update()
    {
        if (guardedFrame != Time.frameCount)
        {
            queuedThisFrame.Clear();
            guardedFrame = Time.frameCount;
        }

        if (queue.Count == 0)
            return;

        int toProcess = Mathf.Min(maxProcessPerFrame, queue.Count);
        for (int i = 0; i < toProcess; i++)
        {
            ProcEvent ev = queue[i];
            ProcessEvent(ref ev);
        }

        queue.RemoveRange(0, toProcess);
    }

    void ProcessEvent(ref ProcEvent ev)
    {
        if (ev.target == null || ev.entries == null)
            return;

        int targetId = ev.target.GetInstanceID();

        for (int i = 0; i < ev.entries.Count; i++)
        {
            EffectEntry entry = ev.entries[i];
            if (entry == null || entry.action == null)
                continue;

            if (entry.trigger != ProcTrigger.Any && entry.trigger != ev.ctx.trigger)
                continue;

            if (entry.procChance < 1f && Random.value > entry.procChance)
                continue;

            if (entry.cooldownSeconds > 0f)
            {
                CooldownKey cooldownKey = new CooldownKey(targetId, entry.action.GetInstanceID(), ev.ctx.trigger);
                if (lastTriggerTimeByKey.TryGetValue(cooldownKey, out float lastTriggerTime))
                {
                    if (Time.time - lastTriggerTime < entry.cooldownSeconds)
                        continue;
                }

                lastTriggerTimeByKey[cooldownKey] = Time.time;
            }

            if (!entry.action.CanExecute(ev.source, ev.target, ev.ctx))
                continue;

            entry.action.Execute(ev.source, ev.target, ev.ctx);
        }
    }

    public void QueueProc(Character_Properties source, Zombie_Properies target, EffectData effects, ProcContext ctx)
    {
        if (effects == null || effects.entries == null)
            return;

        int entriesKey = effects.GetInstanceID();
        QueueProcInternal(source, target, effects.entries, entriesKey, ctx);
    }

    public void QueueProc(Character_Properties source, Zombie_Properies target, IReadOnlyList<EffectEntry> runtimeEntries, int entriesKey, ProcContext ctx)
    {
        if (runtimeEntries == null)
            return;

        QueueProcInternal(source, target, runtimeEntries, entriesKey, ctx);
    }

    void QueueProcInternal(Character_Properties source, Zombie_Properies target, IReadOnlyList<EffectEntry> entries, int entriesKey, ProcContext ctx)
    {
        if (target == null || entries == null || entries.Count == 0)
            return;

        if (queue.Count > 20000)
            return;

        if (guardedFrame != Time.frameCount)
        {
            queuedThisFrame.Clear();
            guardedFrame = Time.frameCount;
        }

        int sourceId = source != null ? source.GetInstanceID() : 0;
        ProcEventKey eventKey = new ProcEventKey(Time.frameCount, target.GetInstanceID(), sourceId, entriesKey, ctx.trigger);
        if (!queuedThisFrame.Add(eventKey))
            return;

        queue.Add(new ProcEvent
        {
            source = source,
            target = target,
            entries = entries,
            ctx = ctx
        });
    }
}
