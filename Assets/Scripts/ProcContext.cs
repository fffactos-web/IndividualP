public enum ProcTrigger
{
    Any = 0,
    OnHit = 1,
    OnDamaged = 2
}

public struct ProcContext
{
    public ProcTrigger trigger;
    public float damageDone;
    public bool isCrit;
    public int hitLayer;
}
