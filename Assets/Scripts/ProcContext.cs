using UnityEngine;

public enum ProcTrigger
{
    Any = 0,
    OnHit = 1,
    OnKill = 2,
    OnDeath = 3,
    OnDamaged = 4,
    OnSpawn = 5
}

public enum ProcDamageType
{
    Direct = 0,
    DamageOverTime = 1,
    Explosion = 2,
    Proc = 3
}

public struct ProcContext
{
    public ProcTrigger trigger;
    public float damageDone;
    public bool isCrit;
    public int hitLayer;

    public bool targetWasKilled;
    public float targetHealthBefore;
    public float targetHealthAfter;

    public Vector3 hitPosition;
    public Character_Properties attacker;
    public Zombie_Properies victim;
    public ProcDamageType damageType;
}
