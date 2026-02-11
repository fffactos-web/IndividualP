using System;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UIElements;

public class Character_StatusBar : MonoBehaviour
{
    public Action<float, Zombie_Properies> OnGetDamage;

    public static Character_StatusBar I;

    Character_Properties c;

    public GameObject inventoryUI;

    [SerializeField] GameObject itemCellPrefab;

    public Dictionary<HeroItemDefinition, int> inventory;

    private void Awake()
    {
        I = this;
        c = GetComponent<Character_Properties>();
        inventory = new Dictionary<HeroItemDefinition, int>();
    }

    public void AddItem(HeroItemDefinition item)
    {
        if (inventory.TryGetValue(item, out int count))
        {
            inventory[item] = count + 1;
            foreach (var itemm in inventoryUI.GetComponentsInChildren<RectTransform>())
            {
                if(itemm.name == item.ItemName)
                {
                    itemm.GetComponentInChildren<TextMeshProUGUI>().text = (count + 1).ToString();
                    inventoryUI.GetComponent<AdaptiveGridFitter>().RebuildCells();
                }
            }
        }
        else
        {
            GameObject cell = Instantiate(itemCellPrefab, inventoryUI.transform);
            cell.name = item.ItemName;
            cell.GetComponent<UnityEngine.UI.Image>().sprite = item.Icon;
            cell.GetComponentInChildren<TextMeshProUGUI>().text = "";
            inventoryUI.GetComponent<AdaptiveGridFitter>().RebuildCells();
            inventory.Add(item, 1);
        }
    }

}
