using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "New Shop Item", menuName = "Shop/Item", order = 1)]
public class ShopItemInfo : ScriptableObject
{
    public string shopItemName;

    public string displayName;
    public int worth;
    public Sprite image;
    public string description;
    public string itemCategoryType;

    public bool mutliPurchasable;
    public string roleItemType;

    public bool favourite;
    public bool onSpecial;

    public string itemName;
}
