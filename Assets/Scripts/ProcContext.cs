public enum ProcDamageType
{
    Direct,
    DoT,
    Explosion,
    Proc
}

public struct ProcContext
{
    public float damageDone;
    public bool isCrit;
    public int hitLayer;

    public bool didKill;
    public float finalDamage;
    public Character_Properties attacker;
    public Zombie_Properies victim;
    public ProcDamageType damageType;
}
