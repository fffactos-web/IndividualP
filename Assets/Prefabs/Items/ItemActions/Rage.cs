using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "BJacket", menuName = "Game/Rage", order = 10)]
public class Rage : HeroItemDefinition
{
    public override void OnEquip()
    {
        Character_StatusBar.I.OnDie += RageReset;
    }

    public void RageReset(Character_Properties c)
    {
        c.died = false;
        c.baseStats.maxHealth = c.baseStats.maxHealth / 2;
        c.Heal(c.baseStats.maxHealth);
        foreach (var overlapped in Physics.OverlapSphere(c.gameObject.transform.position, c.baseStats.damage))
        {
            Zombie_Properies z = overlapped.GetComponent<Zombie_Properies>();
            if (z != null)
            {
                z.Die();
            }
        }
            
    }
}
