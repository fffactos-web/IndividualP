# Negative Effects System

## Архитектура

Система состоит из 4 частей:

1. `EffectAction` — базовый ScriptableObject для логики эффекта.
2. `EffectEntry` — конфиг одного прока (триггер, шанс, cooldown, action).
3. `EffectData` — набор `EffectEntry`, который вешается на оружие/зомби/предмет.
4. `ProcManager` — централизованный обработчик очереди эффектов.

## Поток обработки

1. В боевом коде вызывается `Zombie_Properies.TakeDamage(...)`.
2. Собирается `ProcContext` (урон, крит, источник, флаг убийства и т.д.).
3. `ProcManager` получает событие через `QueueProc(...)`.
4. Для каждого `EffectEntry` порядок строгий:
   - фильтр по `trigger`
   - проверка `procChance`
   - проверка `cooldownSeconds`
   - `CanExecute(...)`
   - `Execute(...)`

## Как создать новый эффект

### 1) Создай новый класс-наследник `EffectAction`

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Actions/My Custom Effect")]
public class MyCustomEffectAction : EffectAction
{
    public float power = 10f;

    public override bool CanExecute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        return target != null && power > 0f;
    }

    public override void Execute(Character_Properties source, Zombie_Properies target, ProcContext ctx)
    {
        // Твоя логика эффекта
        target.InternalApplyDamage(power);
    }
}
```

### 2) Создай asset эффекта

В Unity: `Create -> Effects -> Actions -> My Custom Effect`.

### 3) Привяжи action к `EffectEntry`

- `trigger`: когда срабатывает (`OnHit`, `OnKill`, `OnDeath`, ...)
- `procChance`: шанс срабатывания
- `cooldownSeconds`: кулдаун эффекта
- `action`: созданный ScriptableObject

### 4) Добавь `EffectEntry` в `EffectData`

`EffectData` можно назначить:
- на оружие (`Gun.effectData`, `Rocket.effectData`)
- на цели (`Zombie_Properies.onDamageEffects`)
- в `Character_Properties.activeEffects` (эффекты от предметов)

## Примеры готовых действий

- `PoisonOnHitAction` — яд по шансу с режимами стака (`Refresh/Stack/Ignore`).
- `ExplosionOnKillAction` — взрыв по убийству в радиусе.
- `BonusGemsOnDeathAction` — доп. гемы при убийстве цели.

## Важные замечания

- Для DoT и других внутренних источников урона используй `ProcDamageType`.
- Не создавай `ScriptableObject` в runtime на каждый удар: передавай runtime-списки эффектов напрямую в `ProcManager`.
- Для уникального кулдауна используется ключ `target + action + trigger`, а не индекс массива.
