using UnityEngine;

[CreateAssetMenu(fileName = "BJacket", menuName = "Game/PVT", order = 10)]

public class PVT : HeroItemDefinition
{
    public override void OnEquip()
    {
        Character_StatusBar.I.OnHit += RawHeal;
    }

    public void RawHeal(float dmg, Zombie_Properies z, Character_Properties c)
    {
        
        c.Heal(dmg*c.sBar.inventory[this]/80);
        Debug.Log(dmg*c.sBar.inventory[this]/80);
    }
}
