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

    struct CooldownKey
    {
        public int targetId;
        public int actionInstanceId;
        public int triggerType;

        public CooldownKey(int targetId, int actionInstanceId, int triggerType)
        {
            this.targetId = targetId;
            this.actionInstanceId = actionInstanceId;
            this.triggerType = triggerType;
        }
    }

    // Вместо выделений/копирований под каждый тик: List<ProcEvent> + struct'ы
    List<ProcEvent> queue = new List<ProcEvent>(256);

    // Кулдауны: (targetId + actionInstanceId + triggerType) -> lastTime
    Dictionary<CooldownKey, float> lastTriggerTimeByKey = new Dictionary<CooldownKey, float>();

    [Tooltip("Макс. procs обработается за кадр")]
    public int maxProcessPerFrame = 128;

    void Awake()
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
        int triggerType = ev.ctx.hitLayer;

        for (int i = 0; i < ev.effects.entries.Length; i++)
        {
            var entry = ev.effects.entries[i];
            if (entry.action == null) continue;
            if (Random.value > entry.procChance) continue;

            if (entry.cooldownSeconds > 0f)
            {
                int actionInstanceId = entry.action.GetInstanceID();
                var cooldownKey = new CooldownKey(targetId, actionInstanceId, triggerType);

                if (lastTriggerTimeByKey.TryGetValue(cooldownKey, out float lastTriggerTime) &&
                    Time.time - lastTriggerTime < entry.cooldownSeconds)
                {
                    continue;
                }

                lastTriggerTimeByKey[cooldownKey] = Time.time;
            }

            // Передаём контекст в Action для условий, доп. логики (крит, урон и т.д.)
            entry.action.Execute(ev.source, ev.target, ev.ctx);
        }
    }

    // Вызов из места, где вы знаете цель; очередь пакетит, struct-элементы и минимум GC
    public void QueueProc(Zombie_Properies target, EffectData effects, ProcContext ctx)
    {
        if (effects == null || target == null) return;
        // защита очереди от разрастания
        if (queue.Count > 20000) return;

        queue.Add(new ProcEvent { target = target, effects = effects, ctx = ctx });
    }
}
