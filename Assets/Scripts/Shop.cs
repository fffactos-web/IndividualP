using UnityEngine;
using System.Collections.Generic;

public class Shop : MonoBehaviour
{

    public static Shop I;

    [SerializeField] ShopItem[] shopItems;
    [SerializeField] List<HeroItemDefinition> items;
    List<HeroItemDefinition> commonItems;
    List<HeroItemDefinition> uncommonItems;
    List<HeroItemDefinition> rareItems;
    List<HeroItemDefinition> epicItems;
    List<HeroItemDefinition> mythicItems;
    List<HeroItemDefinition> legendaryItems;

    private void Awake()
    {
        I = this;

        commonItems = new List<HeroItemDefinition>();
        uncommonItems = new List<HeroItemDefinition>();
        rareItems = new List<HeroItemDefinition>();
        epicItems = new List<HeroItemDefinition>();
        mythicItems = new List<HeroItemDefinition>();
        legendaryItems = new List<HeroItemDefinition>();

        foreach (var item in items)
        {
            switch (item.Rarity)
            {
                case ItemRarity.Common: commonItems.Add(item); break;
                case ItemRarity.Uncommon: uncommonItems.Add(item); break;
                case ItemRarity.Rare: rareItems.Add(item); break;
                case ItemRarity.Epic: epicItems.Add(item); break;
                case ItemRarity.Mythic: mythicItems.Add(item); break;
                case ItemRarity.Legendary: legendaryItems.Add(item); break;
            }
        }
    }


    ItemRarity RollRarity(float luck)
    {
        float roll = Random.Range(0f, 1000f);

        roll += luck * 5f;

        roll = Mathf.Clamp(roll, 0f, 1000f);

        if (roll < 600f) return ItemRarity.Common;
        if (roll < 800f) return ItemRarity.Uncommon;
        if (roll < 900f) return ItemRarity.Rare;
        if (roll < 950f) return ItemRarity.Epic;
        if (roll < 975f) return ItemRarity.Mythic;
        return ItemRarity.Legendary;
    }


    HeroItemDefinition GetRandomItemByRarity(ItemRarity rarity)
    {
        List<HeroItemDefinition> source = rarity switch
        {
            ItemRarity.Common => commonItems,
            ItemRarity.Uncommon => uncommonItems,
            ItemRarity.Rare => rareItems,
            ItemRarity.Epic => epicItems,
            ItemRarity.Mythic => mythicItems,
            ItemRarity.Legendary => legendaryItems,
            _ => commonItems
        };

        if (source.Count == 0)
            return null;

        return source[Random.Range(0, source.Count)];
    }

    public void RefreshItems()
    {
        Character_Properties c = GameObject.FindGameObjectWithTag("Player").GetComponent<Character_Properties>();

        float luck = c.GetStats().luck;

        for (int i = 0; i < 3; i++)
        {
            ItemRarity rarity = RollRarity(luck);
            HeroItemDefinition item = GetRandomItemByRarity(rarity);

            if (item != null)
            {
                shopItems[i].SetItemVisual(item);
            }
        }
    }

}
