
using UnityEngine;

[CreateAssetMenu(fileName = "BJacket", menuName = "Game/Hero Item/BJacket", order = 10)]
public class BJacket : HeroItemDefinition
{
    Character_StatusBar c;

    public override void OnEquip()
    {
        Character_StatusBar.I.OnGetDamage += ReflectDamage;
    }

    public void ReflectDamage(float damage, Zombie_Properies zombie)
    {
        zombie.GetDamage(damage);
    }
}
