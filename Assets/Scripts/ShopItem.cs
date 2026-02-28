using TMPro;
using UnityEngine;

public class ShopItem : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Image background;
    [SerializeField] UnityEngine.UI.Image itemIcon;
    [SerializeField] TextMeshProUGUI itemDescription;
    [SerializeField] TextMeshProUGUI itemName;
    [SerializeField] TextMeshProUGUI itemCost;

    HeroItemDefinition item;
    public static ShopItem I;

    private void Awake()
    {
        I = this;
    }

    public void SetItemVisual(HeroItemDefinition itemDefinition)
    {
        item = itemDefinition;
        itemIcon.sprite = itemDefinition.Icon;
        switch (itemDefinition.Rarity)
        {
            case ItemRarity.Common:
                background.color = Color.gray;
                break;
            case ItemRarity.Uncommon:
                background.color = Color.green;
                break;
            case ItemRarity.Rare:
                background.color = Color.blue;
                break;
            case ItemRarity.Epic:
                background.color = new Color(1, 0, 0.6f);
                break;
            case ItemRarity.Mythic:
                background.color = Color.red;
                break;
            case ItemRarity.Legendary:
                background.color = new Color(1, 0.85f, 0f);
                break;
            default:
                break;
        }
        itemDescription.text = itemDefinition.Description;
        itemName.text = itemDefinition.ItemName;
        itemCost.text = itemDefinition.Cost.ToString();
    }

    public void Choose()
    {
        Character_Properties c = GameObject.FindGameObjectWithTag("Player").GetComponent<Character_Properties>();
        c.ApplyItem(item);

        Character_StatusBar.I.AddItem(item);

        Time.timeScale = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked; 
        Shop.I.gameObject.SetActive(false);
    }

}
