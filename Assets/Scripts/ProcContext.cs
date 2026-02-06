// Маленькая struct-структура для передачи контекста proc'а без аллокаций.
public struct ProcContext
{
    public float damageDone;
    public bool isCrit;
    public int hitLayer;
    // расширяй по потребности (weaponId, projectileSpeed и т.п.)
}
