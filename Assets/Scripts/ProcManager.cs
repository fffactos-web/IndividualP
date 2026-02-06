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

    // î÷åðåäü áåç ïîñòîÿííî ñîçäàâàåìûõ îáúåêòîâ: List<ProcEvent> õðàíèò struct'û
    List<ProcEvent> queue = new List<ProcEvent>(256);

    // äëÿ cooldown'îâ: (targetInstanceId -> entryIndex -> lastTime)
    Dictionary<int, float[]> lastTriggerTime = new Dictionary<int, float[]>();

        Character_Properties resolvedSource = ev.source;
        if (resolvedSource == null)
        {
            resolvedSource = FindFirstObjectByType<Character_Properties>();
            if (resolvedSource != null)
                Debug.LogWarning("ProcManager: QueueProc called without source. Using fallback Character_Properties.");
        }

            entry.action.Execute(resolvedSource, ev.target, ev.ctx);
    public void QueueProc(Character_Properties source, Zombie_Properies target, EffectData effects, ProcContext ctx)

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
        if (ev.effects == null || ev.effects.entries == null || ev.target == null) return;

        int targetId = ev.target.GetInstanceID();

        if (!lastTriggerTime.TryGetValue(targetId, out float[] timers))
        {
            timers = new float[ev.effects.entries.Length];
            for (int k = 0; k < timers.Length; k++)
                timers[k] = -9999f;

            lastTriggerTime[targetId] = timers;
        }

        for (int i = 0; i < ev.effects.entries.Length; i++)
        {
            var entry = ev.effects.entries[i];
            if (entry.action == null) continue;
            if (Random.value > entry.procChance) continue;

            if (entry.cooldownSeconds > 0f)
            {
                if (i >= timers.Length)
                {
                    // åñëè ìåíÿåòñÿ äëèíà entries ìåæäó âûçîâàìè — îáíîâèì ìàññèâ
                    var newArr = new float[ev.effects.entries.Length];
                    for (int j = 0; j < Mathf.Min(newArr.Length, timers.Length); j++) newArr[j] = timers[j];
                    for (int j = timers.Length; j < newArr.Length; j++) newArr[j] = -9999f;
                    timers = newArr;
                    lastTriggerTime[targetId] = timers;
                }

                if (Time.time - timers[i] < entry.cooldownSeconds) continue;
                timers[i] = Time.time;
            }

            // Âûïîëíÿåì äåéñòâèå — Action ñàì çíàåò, ÷òî äåëàåò (ïóëë, äîï. óðîí è ò.ï.)
            entry.action.Execute(ev.source, ev.target, ev.ctx);
        }
    }

    // Âûçîâ — ìîæíî äåëàòü èç ëþáîãî ìåñòà; ïåðåäà¸ì ññûëêè, struct-êîíòåêñò — ìèíèìóì GC
    public void QueueProc(Zombie_Properies target, EffectData effects, ProcContext ctx) 
    {
        if (effects == null || target == null) return;
        // ïðîñòàÿ çàùèòà îò ïåðåïîëíåíèÿ
        if (queue.Count > 20000) return;

        queue.Add(new ProcEvent {target = target, effects = effects, ctx = ctx });
    }
}
