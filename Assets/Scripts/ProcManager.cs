using System.Collections.Generic;
using UnityEngine;

public class ProcManager : MonoBehaviour
{
    public static ProcManager Instance { get; private set; }

    struct ProcEvent
    {
        public Character_Properties source;
        public Zombie_Properies target;
        public EffectData effects;
        public ProcContext ctx;
    }

    readonly List<ProcEvent> queue = new List<ProcEvent>(256);

    // targetInstanceId -> entryIndex -> lastTime
    readonly Dictionary<int, float[]> lastTriggerTime = new Dictionary<int, float[]>();

    // Frame guard against duplicate queueing of the exact same event in the same frame.
    readonly HashSet<ProcEventKey> queuedThisFrame = new HashSet<ProcEventKey>();
    int guardedFrame = -1;

    [Tooltip("Max amount of procs processed per frame")]
    public int maxProcessPerFrame = 128;

    readonly struct ProcEventKey
    {
        public readonly int frame;
        public readonly int targetId;
        public readonly int effectsId;
        public readonly ProcTrigger trigger;
        public readonly int damageBits;
        public readonly bool isCrit;
        public readonly int hitLayer;

        public ProcEventKey(int frame, int targetId, int effectsId, ProcContext ctx)
        {
            this.frame = frame;
            this.targetId = targetId;
            this.effectsId = effectsId;
            trigger = ctx.trigger;
            damageBits = System.BitConverter.SingleToInt32Bits(ctx.damageDone);
            isCrit = ctx.isCrit;
            hitLayer = ctx.hitLayer;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Update()
    {
        if (guardedFrame != Time.frameCount)
        {
            queuedThisFrame.Clear();
            guardedFrame = Time.frameCount;
        }

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
        if (ev.effects == null || ev.effects.entries == null || ev.target == null) return;

        int targetId = ev.target.GetInstanceID();

        if (!lastTriggerTime.TryGetValue(targetId, out float[] timers))
        {
            timers = new float[ev.effects.entries.Length];
            for (int i = 0; i < timers.Length; i++) timers[i] = -9999f;
            lastTriggerTime[targetId] = timers;
        }

        for (int i = 0; i < ev.effects.entries.Length; i++)
        {
            var entry = ev.effects.entries[i];
            if (entry == null || entry.action == null) continue;

            // Unified order: trigger filter -> chance -> cooldown -> execute.
            if (entry.trigger != ProcTrigger.Any && entry.trigger != ev.ctx.trigger) continue;
            if (Random.value > entry.procChance) continue;

            if (entry.cooldownSeconds > 0f)
            {
                if (i >= timers.Length)
                {
                    var newArr = new float[ev.effects.entries.Length];
                    for (int j = 0; j < Mathf.Min(newArr.Length, timers.Length); j++) newArr[j] = timers[j];
                    for (int j = timers.Length; j < newArr.Length; j++) newArr[j] = -9999f;
                    timers = newArr;
                    lastTriggerTime[targetId] = timers;
                }

                if (Time.time - timers[i] < entry.cooldownSeconds) continue;
                timers[i] = Time.time;
            }

            entry.action.Execute(ev.source, ev.target, ev.ctx);
        }
    }

    public void QueueProc(Zombie_Properies target, EffectData effects, ProcContext ctx)
    {
        if (effects == null || target == null) return;
        if (queue.Count > 20000) return;

        if (guardedFrame != Time.frameCount)
        {
            queuedThisFrame.Clear();
            guardedFrame = Time.frameCount;
        }

        var key = new ProcEventKey(Time.frameCount, target.GetInstanceID(), effects.GetInstanceID(), ctx);
        if (!queuedThisFrame.Add(key)) return;

        queue.Add(new ProcEvent { target = target, effects = effects, ctx = ctx });
    }
}
