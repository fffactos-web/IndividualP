using System;
using UnityEngine;
using System.Collections.Generic;

public class Character_StatusBar : MonoBehaviour
{
    public Action<float, Zombie_Properies> OnGetDamage;

    public static Character_StatusBar I;

    Character_Properties c;

    public Dictionary<HeroItemDefinition, int> inventory;

    private void Awake()
    {
        I = this;
        c = GetComponent<Character_Properties>();
        inventory = new Dictionary<HeroItemDefinition, int>();
    }

    public void OnKillAction(Transform killedTransform)
    {
        
    }

    public void OnGetDamageAction(Zombie_Properies zombie)
    {
    }

}
