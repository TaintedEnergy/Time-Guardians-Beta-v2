using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ShopContents
{
    public string shopName;

    public List<ShopItemInfo> allItems = new List<ShopItemInfo>();
    public List<ShopItemInfo> weaponsItems = new List<ShopItemInfo>();
    public List<ShopItemInfo> supportItems = new List<ShopItemInfo>();
    public List<ShopItemInfo> miscItems = new List<ShopItemInfo>();
}
