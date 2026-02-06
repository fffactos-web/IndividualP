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

    // очередь без посто€нно создаваемых объектов: List<ProcEvent> хранит struct'ы
    List<ProcEvent> queue = new List<ProcEvent>(256);

    // дл€ cooldown'ов: (targetInstanceId -> entryIndex -> lastTime)
    Dictionary<int, float[]> lastTriggerTime = new Dictionary<int, float[]>();

    [Tooltip("ћакс. procs обработать за кадр")]
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
                    // если мен€етс€ длина entries между вызовами Ч обновим массив
                    var newArr = new float[ev.effects.entries.Length];
                    for (int j = 0; j < Mathf.Min(newArr.Length, timers.Length); j++) newArr[j] = timers[j];
                    for (int j = timers.Length; j < newArr.Length; j++) newArr[j] = -9999f;
                    timers = newArr;
                    lastTriggerTime[targetId] = timers;
                }

                if (Time.time - timers[i] < entry.cooldownSeconds) continue;
                timers[i] = Time.time;
            }

            // ¬ыполн€ем действие Ч Action сам знает, что делает (пулл, доп. урон и т.п.)
            entry.action.Execute(ev.source, ev.target, ev.ctx);
        }
    }

    // ¬ызов Ч можно делать из любого места; передаЄм ссылки, struct-контекст Ч минимум GC
    public void QueueProc(Zombie_Properies target, EffectData effects, ProcContext ctx) 
    {
        if (effects == null || target == null) return;
        // проста€ защита от переполнени€
        if (queue.Count > 20000) return;

        queue.Add(new ProcEvent {target = target, effects = effects, ctx = ctx });
    }
}
