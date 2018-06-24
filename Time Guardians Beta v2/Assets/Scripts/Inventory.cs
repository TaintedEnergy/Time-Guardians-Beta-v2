﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Inventory : NetworkBehaviour
{
    [Header("Main Components")]

    public string[] slotOrder;

    public InvSlotInfo[] items;
    public ItemInfo[] itemInfos;

    public int selected;

    [SerializeField] GameObject pickUp;
    public GameObject dropPosition;
    [SerializeField] float dropSpeed;

    Player player;

    [Header("Shop Components")]

    public int crystals;


    void Start ()
    {
        player = GetComponent<Player>();

        ResetInv();
        if (isServer)
        {
            Invoke("ResetInv", 3f);
        }
    }

    void Update()
    {
        #region Button Pushes

        if (Input.GetKeyDown("1"))
        {
            SelectItem(0, false);
        }
        if (Input.GetKeyDown("2"))
        {
            SelectItem(1, false);
        }
        if (Input.GetKeyDown("3"))
        {
            SelectItem(2, false);
        }
        if (Input.GetKeyDown("4"))
        {
            SelectItem(3, false);
        }
        if (Input.GetKeyDown("5"))
        {
            SelectItem(4, false);
        }
        if (Input.GetKeyDown("6"))
        {
            SelectItem(5, false);
        }
        if (Input.GetKeyDown("7"))
        {
            SelectItem(6, false);
        }
        if (Input.GetKeyDown("8"))
        {
            SelectItem(7, false);
        }
        if (Input.GetKeyDown("9"))
        {
            SelectItem(8, false);
        }
        if (Input.GetKeyDown("0"))
        {
            SelectItem(9, false);
        }
        #endregion

        // Trying to drop item
        if (Input.GetKeyDown("q") && items[selected] != null && items[selected].itemName != "empty" && items[selected].itemName != null && items[selected].itemName != "")
        {
            for (int i = 0; i < player.playerShooting.firstItem.Length; i++)
            {
                if (player.playerShooting.firstItem[i].GetComponent<ItemInfo>().itemName == items[selected].itemName && player.playerShooting.firstItem[i].GetComponent<ItemInfo>().canDrop)
                {
                    DropItem(false);
                }
            }
        }

        // Scroll Wheel
        if (!PlayerCanvas.canvas.shopObject.activeInHierarchy)
        {
            if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                int max = 5;
                if (ExtraItemCheck())
                {
                    max += 5;
                }

                int newSelection = selected + 1;
                if (newSelection == max) { newSelection = 0; }

                SelectItem(newSelection, false);
            }
            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                int max = 5;
                if (ExtraItemCheck())
                {
                    max += 5;
                }

                int newSelection = selected - 1;
                if (newSelection == -1) { newSelection = max-1; }

                SelectItem(newSelection, false);
            }
        }
    }

    public void ResetInv ()
    {
        for (int i = 0; i < 10; i++)
        {
            NewItem(i, "empty");
        }
        SelectItem(0, true);
    }

    public void NewItem (int slot, string newItemName)
    {
        items[slot] = new InvSlotInfo();

        items[slot].itemName = newItemName;
        // Get Weapon Info
        for (int i = 0; i < itemInfos.Length; i++)
        {
            if (itemInfos[i].itemName == items[slot].itemName)
            {
                items[slot].clipAmmo = itemInfos[i].maxclipSize;
                items[slot].totalAmmo = itemInfos[i].maxclipSize;
            }
        }

        PlayerCanvas.canvas.NewItem(slot, newItemName);

        if (slot == selected)
        {
            SelectItem(slot, false);
        }

        // Show extra slots if extra item present
        if (ExtraItemCheck() && !PlayerCanvas.canvas.extraItems.activeInHierarchy)
        {
            PlayerCanvas.canvas.extraItems.SetActive(true);
        }
        if (!ExtraItemCheck() && PlayerCanvas.canvas.extraItems.activeInHierarchy)
        {
            PlayerCanvas.canvas.extraItems.SetActive(false);
        }
    }

    void DropItem(bool forced)
    {
        if (Player.player != null && (forced || (!forced && Player.player.playerShooting.elapsedTime == 0)))
        {
            CmdDropItem(items[selected].itemName);
            NewItem(selected, "empty");
        }
    }


    // Check for atleast 1 extra item in your inventory
    bool ExtraItemCheck()
    {
        bool value = false;

        for (int i = 0; i < 5; i++)
        {
            if (items[i + 5] != null && items[i + 5].itemName != "empty" && items[i + 5].itemName != "" && items[i + 5].itemName != null)
            {
                value = true;
            }
        }

        return value;
    }

    public void SelectItem (int value, bool forced)
    {
        if (Player.player != null && (forced || (!forced && Player.player.playerShooting.elapsedTime == 0)))
        {
            // Dont select extra slot if no extra items
            if (value < 5 || (value >= 5 && ExtraItemCheck()))
            {
                int clipSize = -1;
                string displayName = "";
                // Get Weapon Info
                for (int i = 0; i < itemInfos.Length; i++)
                {
                    if (itemInfos[i].itemName == items[value].itemName)
                    {
                        clipSize = itemInfos[i].maxclipSize;
                        displayName = itemInfos[i].displayedName;
                    }
                }

                PlayerCanvas.canvas.NewSelection(value, displayName);

                PlayerCanvas.canvas.NewAmmo(items[value].clipAmmo, clipSize, items[value].totalAmmo);
                selected = value;

                player.playerShooting.GetItem(items[value].itemName);
            }
            else if (selected >= 5)
            {
                selected = 0;
                SelectItem(0, false);
            }
        }
    }

    void UpdateItemSlot ()
    {

    }
    
    public void CollectNewItem(int pickUpId, string itemName, string itemType)
    {
        int slot = -1;
        for (int i = 0; i < slotOrder.Length; i++)
        {
            if (slotOrder[i] == itemType && items[i].itemName == "empty")
            {
                slot = i;

                NewItem(slot, itemName);

                PickUp.disable = pickUpId;
                Player.player.CmdRequestPickupDestroy(pickUpId);

                // End
                i = slotOrder.Length;
            }
        }
    }

    public void Die()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].itemName != "empty" && items[i].itemName != null && items[i].itemName != "")
            {
                for (int a = 0; a < player.playerShooting.firstItem.Length; a++)
                {
                    if (player.playerShooting.firstItem[a].GetComponent<ItemInfo>().itemName == items[i].itemName && player.playerShooting.firstItem[a].GetComponent<ItemInfo>().dropOnDeath)
                    {
                        CmdDropItem(items[i].itemName);
                        NewItem(i, "empty");
                    }
                }
            }
        }
        
        SelectItem(0, true);
    }
    
    [Command]
    public void CmdDropItem(string itemName)
    {
        if (Player.player != null)
        {
            Player.player.cmds++;
        }

        GameObject newPickup = Instantiate(pickUp, dropPosition.transform.position, dropPosition.transform.rotation);
        NetworkServer.Spawn(newPickup);

        newPickup.GetComponent<PickUp>().itemNameSpawn = itemName;
        newPickup.GetComponent<PickUp>().delay = 100;
        newPickup.GetComponent<PickUp>().startVelocity = transform.forward * dropSpeed;
    }
}