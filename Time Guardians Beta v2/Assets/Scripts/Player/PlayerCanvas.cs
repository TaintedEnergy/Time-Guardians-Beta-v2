using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections.Generic;

public class PlayerCanvas : NetworkBehaviour
{
    public static PlayerCanvas canvas;
    
    public Text fpsText;
    int fpsTime;

    public Text cmdsText;
    public Text rpcsText;


    [Header("Main Components")]

    public Slider healthBarSlider;
    public Text healthText;

    public int countdownTime;
    public Text countdownText;

    public Image[] bloodScreens;

    bool masked;
    public Animator overlayAnim;


    [Header("Reticule Components")]

    public GameObject reticule;
    public RectTransform[] reticuleLines;

    public float reticuleSpacing;
    float lastReticuleSpacing;
    float reticuleLerpSpeed;

    float reticuleElapsedTime;


    [Header("View Components")]

    public string viewName;
    public int viewHealth;

    public GameObject viewObject;

    public Text viewNameText;
    public Text viewHealthText;
    public Image viewImage;

    public HurtInfo[] hurtInfos;


    [Header("Overlay Components")]

    public GameObject scopeImage;
    public GameObject flashImage;
    public GameObject smokeImage;

    int flashTime;
    int bangTime;
    public bool smoked;
    [SerializeField] int smokeTime;


    [Header("Inventory Components")]

    public GameObject inventoryObject;

    public Text itemText;
    public Image[] slotBoxes;
    public Image[] slotIcons;
    public Image[] slotImages;

    public GameObject extraItems;

    public Text clipText;
    public Text ammoText;

    public ImageNameInfo[] imageReferences;


    [Header("Roles")]

    public Text roleText;
    public Text roleTextback;
    public Image roleImage;

    public SyncListString ids;
    public SyncListString roles;

    public GameObject tabMenu;
    public float tabMenuWidth = 300f;
    public GameObject tabItemAsset;
    
    public List<TabInfo> tabItemsInfo = new List<TabInfo>();

    [Header("Shop")]

    public string shopType;
    public string shopViewType;
    public GameObject shopObject;
    public GameObject shopPanel;
    public Image[] coloredShopImages;
    public GameObject shopIcon;
    public GameObject IconHolder;

    List<GameObject> icons = new List<GameObject>();

    public string currentCategory;
    public int currentSortMethod;
    public int[] sortMethodDirection = new int[3];
    public GameObject[] sortIcons;

    // From old ShopController Script

    public static ShopContents selectedShop;
    public ShopItemInfo[] allShopItems;

    int lastShopIndex;
    float lastShopClick = -1;

    public Text crystalText;
    public Text shopItemNameText;
    public Text shopItemDescriptionText;
    public Text shopItemWorthText;

    [Header("Item Menu Components")]
    
    public GameObject c4Panel;
    public C4 c4;
    public GameObject c4ArmButton;
    public GameObject c4PlantButton;
    public GameObject c4ArmedText;
    public GameObject c4PlantedText;
    public Text c4MinuteText;
    public Text c4SecondText;


    [Header("Body Inspection")]

    public string bodyName;

    public GameObject bodyObject;
    public GameObject bodyPanel;

    public Text bodyNameText;
    public Image bodyRoleImage;
    public Text bodyQuestionMark;

    public float elapsedBodyTime = 0;


    [Header("Game Over")]

    public GameObject gameOverObject;

    public Text goWinTeamText;
    public Image goRoleImage;
    public Text goWinTeamName;
    public Text goWinPlayers;

    // Ensure there is only one PlayerCanvas
    void Awake()
    {
        canvas = this;
    }

    void Update()
    {
        if (fpsTime >= 100)
        {
            fpsText.text = "FPS: " + (int)(1.0 / Time.deltaTime);
            fpsTime = 0;

            if (Player.player != null)
            {
                cmdsText.text = "Cmds: " + Player.player.cmds;
                rpcsText.text = "Rpcs: " + Player.player.rpcs;

                Player.player.cmds = 0;
                Player.player.rpcs = 0;
            }
        }

        // Mask Toggling
        if (Player.player != null)
        {
            if (Player.player.alive)
            {
                if (Input.GetKeyDown("c") && Player.player.clientRole != "jester")
                {
                    Player.player.ToggleMasked();
                }
                if (!Player.player.masked && overlayAnim.GetInteger("State") != 0)
                {
                    overlayAnim.SetInteger("State", 0);
                }
                if (Player.player.masked)
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        overlayAnim.SetInteger("State", 3);
                    }
                    else if (Input.GetKey("w") || Input.GetKey("a") || Input.GetKey("s") || Input.GetKey("d"))
                    {
                        overlayAnim.SetInteger("State", 2);
                    }
                    else
                    {
                        overlayAnim.SetInteger("State", 1);
                    }
                }
            }
            else if (overlayAnim.GetInteger("State") != 0)
            {
                overlayAnim.SetInteger("State", 0);
            }
        }

        // Set Crystal Text
        if (Player.player != null && Player.player.inventory != null)
        {
            if (crystalText.text != "Time Vortex Crystals Collected: " + Player.player.inventory.crystals.ToString())
            {
                crystalText.text = "Time Vortex Crystals Collected: " + Player.player.inventory.crystals.ToString();
            }
        }
    }

    private void FixedUpdate()
    {
        fpsTime++;
        if (elapsedBodyTime > 0)
        {
            elapsedBodyTime -= Time.deltaTime;
        }
        if (reticuleSpacing != lastReticuleSpacing)
        {
            LerpReticuleSize();
        }

        Flashbanging();
        Smoking();
    }

    public void EditReticuleSize(float value, float lerpValue)
    {
        if (value == -1)
        {
            reticule.SetActive(false);

            reticuleSpacing = -1;
            lastReticuleSpacing = -1;
        }
        else if (value != reticuleSpacing)
        {
            reticule.SetActive(true);

            lastReticuleSpacing = reticuleSpacing;
            reticuleSpacing = value;
            reticuleElapsedTime = 0;

            // If has lerp value, then lerp
            if (lerpValue > 0)
            {
                reticuleLerpSpeed = lerpValue;
            }
            else // Just set
            {
                for (int i = 0; i < reticuleLines.Length; i++)
                {
                    if (i == 0) { reticuleLines[i].transform.localPosition = new Vector3(-value, 0, 0); }
                    if (i == 1) { reticuleLines[i].transform.localPosition = new Vector3(0, value, 0); }
                    if (i == 2) { reticuleLines[i].transform.localPosition = new Vector3(value, 0, 0); }
                    if (i == 3) { reticuleLines[i].transform.localPosition = new Vector3(0, -value, 0); }
                }

                lastReticuleSpacing = value;
            }
        }
    }

    void LerpReticuleSize()
    {
        reticuleElapsedTime += Time.deltaTime / reticuleLerpSpeed;

        if (reticuleElapsedTime >= 1)
        {
            reticuleLerpSpeed = 0;

            // Ensure exact value
            reticuleElapsedTime = 1;
        }

        float value = Mathf.Lerp(lastReticuleSpacing, reticuleSpacing, reticuleElapsedTime);

        for (int i = 0; i < reticuleLines.Length; i++)
        {
            if (i == 0) { reticuleLines[i].transform.localPosition = new Vector3(-value, 0, 0); }
            if (i == 1) { reticuleLines[i].transform.localPosition = new Vector3(0, value, 0); }
            if (i == 2) { reticuleLines[i].transform.localPosition = new Vector3(value, 0, 0); }
            if (i == 3) { reticuleLines[i].transform.localPosition = new Vector3(0, -value, 0); }
        }

        // Reseting Reticule Values to default (not moving)
        if (reticuleElapsedTime == 1)
        {
            lastReticuleSpacing = reticuleSpacing;

            reticuleElapsedTime = 0;
        }
    }

    public void SetHealth(int amount)
    {
        // Health Text
        healthBarSlider.value = amount;
        healthText.text = amount + "%";
        // Health Color
        for (int i = 0; i < hurtInfos.Length; i++)
        {
            if (amount >= hurtInfos[i].minDamage)
            {
                healthBarSlider.fillRect.GetComponent<Image>().color = hurtInfos[i].color;
                // End
                i = hurtInfos.Length;
            }
        }
    }

    public void GameWinMenu(string roleName, string[] winners)
    {
        RoleInfo roleInfo = ReferenceInfo.referenceInfo.RoleInformation(roleName);

        gameOverObject.SetActive(true);

        /// Set Visuals

        // Role Image
        goRoleImage.sprite = roleInfo.image;

        // Team name
        goWinTeamName.text = roleInfo.displayedName;

        if (winners.Length >= 2)
        {
            goWinTeamName.text = roleInfo.displayedName + "s";
        }

        if (winners.Length >= 1)
        {
            // Winner List
            goWinPlayers.text = winners[0];
            if (winners.Length >= 2)
            {
                for (int i = 1; i < winners.Length; i++)
                {
                    goWinPlayers.text = goWinPlayers.text + "\n" + winners[i];
                }
            }
        }

        inventoryObject.SetActive(false);

        // Disable in 5 seconds
        Invoke("DisableWinMenu", 5f);
    }

    void DisableWinMenu()
    {
        inventoryObject.SetActive(true);
        gameOverObject.SetActive(false);
    }

    public void Countdown ()
    {
        countdownTime--;
        countdownText.text = countdownTime + "";

        if (countdownTime > 0)
        {
            Invoke("Countdown", 1);
        }
    }

    /// <summary>
    /// Item menus
    /// </summary>
    
    public void C4Recieve (bool open, bool arm, bool plant, string minutes, string seconds)
    {
        c4Panel.SetActive(open);

        c4ArmButton.SetActive(!arm);
        c4ArmedText.SetActive(arm);
        c4PlantButton.SetActive(!plant);
        c4PlantedText.SetActive(plant);
        c4MinuteText.text = minutes;
        c4SecondText.text = seconds;
    }

    public void C4Set(int value)
    {
        if (value == 0)
        {
            Player.player.AthorityCall(c4, "CmdArmed", 0);
        }
        if (value == 1)
        {
            Player.player.AthorityCall(c4, "CmdPlanted", 0);
        }
    }

    /// <summary>
    /// Effect Overlays
    /// </summary>

    public void ScopeImage(bool value)
    {
        scopeImage.SetActive(value);
        inventoryObject.SetActive(!value);
    }

    public void Flashbang(int flash, int bang)
    {
        flashImage.SetActive(true);

        flashTime = flash;
        bangTime = bang;
        // Specific #FIX
    }

    void Flashbanging()
    {
        // Flashing (Light)

        if (flashTime > 0)
        {
            // Unflashing
            if (flashTime >= 500 && flashTime <= 800)
            {
                flashImage.GetComponent<Image>().color = new Color(1, 1, 1, 1 - ((float)flashTime - 500) / 300);
            }
            
            // Flashing
            if (flashTime <= 7)
            {
                // flashImage.GetComponent<Image>().color = new Color(1,1,1, (float)flashTime / 5);
                flashImage.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(flashImage.GetComponent<Image>().color.a, 1, flashTime /7f));
            }

            flashTime++;
        }
        // End
        if (flashTime >= 800)
        {
            flashImage.SetActive(false);

            flashTime = 0;
        }

        // Banging (Sound)

        if (bangTime > 0)
        {
            if (bangTime == 10)
            {
                flashImage.GetComponent<AudioSource>().Play();
            }
            if (bangTime >= 30 && bangTime < 80)
            {
                AudioListener.volume = Mathf.Lerp(1,0, (float)(bangTime - 30)/49);
            }
            if (bangTime >= 150 && bangTime < 500)
            {
                AudioListener.volume = (float)(bangTime - 150) / 349;
            }

            bangTime++;
        }
        // End
        if (bangTime >= 500)
        {
            bangTime = 0;
        }
    }

    void Smoking()
    {
        if (smoked && smokeTime < 50)
        {
            smokeTime++;
        }
        if (!smoked && smokeTime > 0)
        {
            smokeTime--;
        }

        Color c = smokeImage.GetComponent<Image>().color;
        smokeImage.GetComponent<Image>().color = new Color(c.r, c.g, c.b, ((float)smokeTime / 50));
    }

    /// <summary>
    /// View Information
    /// </summary>

    public void View(string playerName, int playerHealth, bool masked)
    {
        // If get nothing
        if (playerName == "" || playerName == null)
        {
            if (viewName != "")
            {
                viewName = playerName;
                viewObject.SetActive(false);
            }
        }
        else
        {
            // Got a name, enable view object if needed
            if (!viewObject.activeInHierarchy)
            {
                viewObject.SetActive(true);
            }
            // If player name is not what recieved, change to correct name
            if (viewName != playerName && !masked)
            {
                viewName = playerName;
                viewNameText.text = viewName;
            }
            // If masked, displayed masked name
            if (masked && viewName != "* masked *")
            {
                viewName = "* masked *";
                viewNameText.text = viewName;
            }
            // If player health is not what recieved, change to correct health

            if (viewHealth != playerHealth)
            {
                viewHealth = playerHealth;
                for (int i = 0; i < hurtInfos.Length; i++)
                {
                    if (viewHealth >= hurtInfos[i].minDamage)
                    {
                        viewHealthText.text = hurtInfos[i].hurtName;
                        viewHealthText.color = hurtInfos[i].color;
                        // End
                        i = hurtInfos.Length;
                    }
                }
            }
            // print(Player.player.playerName + " " + viewName);

            // Check and Set Role Image based on current role and masked
            if (!masked && roles.Count != 0)
            {
                RoleInfo roleInfo = null;
                // Get Role Info
                for (int i = 0; i < roles.Count; i++)
                {
                    if (ids[i] == Player.player.playerName)
                    {
                        roleInfo = ReferenceInfo.referenceInfo.RoleInformation(roles[i]);
                        // End
                        i = roles.Count;
                    }
                }
                // Get Viewed Role Info
                RoleInfo viewedRoleInfo = null;
                for (int i = 0; i < roles.Count; i++)
                {
                    if (ids[i] == viewName)
                    {
                        viewedRoleInfo = ReferenceInfo.referenceInfo.RoleInformation(roles[i]);
                        // End
                        i = roles.Count;
                    }
                }

                viewImage.gameObject.SetActive(true);
                if (roleInfo.name != "amnesiac" && viewedRoleInfo.name == "detective")
                {
                    viewImage.sprite = viewedRoleInfo.imageGlow;
                }
                else if (roleInfo.roleWinType == "traitor" && viewedRoleInfo.roleWinType == "traitor")
                {
                    viewImage.sprite = viewedRoleInfo.imageGlow;
                    //ShopController.selectedShop = traitorShop;
                    
                }
                else if (roleInfo.roleWinType != "innocent" && viewedRoleInfo.name == "jester")
                {
                    viewImage.sprite = viewedRoleInfo.imageGlow;
                }
                else
                {
                    viewImage.gameObject.SetActive(false);
                }
            }
            else
            {
                viewImage.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Inventory
    /// </summary>

    public void NewItem (int slot, string newItemName)
    {
        if (newItemName == "empty")
        {
            slotImages[slot].sprite = null;
            slotImages[slot].gameObject.SetActive(false);
            if (slot < 5)
            {
                slotIcons[slot].gameObject.SetActive(true);
            }
        }
        else
        {
            // Get Image Info
            for (int i = 0; i < imageReferences.Length; i++)
            {
                if (newItemName == imageReferences[i].imageName)
                {
                    slotImages[slot].sprite = imageReferences[i].image;
                }
            }

            slotImages[slot].gameObject.SetActive(true);
            if (slot < 5)
            {
                slotIcons[slot].gameObject.SetActive(false);
            }
        }
    }

    public void NewSelection (int slotValue, string itemName)
    {
        itemText.text = itemName;

        for (int i = 0; i < 10; i++)
        {
            slotBoxes[i].color = new Color(1,1,1,0.25f);
            slotImages[i].color = new Color(1, 1, 1, 0.5f);
            if (i < 5) { slotIcons[i].color = new Color(0, 0, 0, 0.58f); }
        }
        slotBoxes[slotValue].color = new Color(1, 1, 1, 1f);
        slotImages[slotValue].color = new Color(1, 1, 1, 1f);
        if (slotValue < 5) { slotIcons[slotValue].color = new Color(0, 0, 0, 0.78f); }
    }

    public void NewAmmo (int clipAmmo, int clipSize, int totAmmo)
    {
        if (totAmmo != -1)
        {
            clipText.text = clipAmmo + "/" + clipSize;
            ammoText.text = totAmmo + "";
        }
        else
        {
            clipText.text = "∞";
            ammoText.text = "∞";
        }
    }

    public void ReceiveDamage(int strength, int direction)
    {
        if (strength == 0)
        {
            bloodScreens[direction].GetComponent<Animator>().SetTrigger("Minor");
        }
        if (strength == 1)
        {
            bloodScreens[direction].GetComponent<Animator>().SetTrigger("Medium");
        }
        if (strength == 2)
        {
            bloodScreens[direction].GetComponent<Animator>().SetTrigger("Major");
        }
    }

    /// <summary>
    /// Sync Roles
    /// </summary>

    public void SyncData (SyncListString syncedRoles, SyncListString syncedIds)
    {
        roles = syncedRoles;
        ids = syncedIds;

        GetRole();
    }

    public void GetRole()
    {
        for (int i = 0; i < roles.Count; i++)
        {
            if (ids[i] == Player.player.playerName)
            {
                RoleInfo roleInfo = ReferenceInfo.referenceInfo.RoleInformation(roles[i]);
                
                SetRoleVisual(roleInfo.name, roleInfo.displayedName, roleInfo.roleColour, roleInfo.textColour, roleInfo.image);
            }
        }
    }

    public void ResetRoleVisuals()
    {
        RoleInfo roleInfo = ReferenceInfo.referenceInfo.rolesInfo[0];

        SetRoleVisual(roleInfo.name, roleInfo.displayedName, roleInfo.roleColour, roleInfo.textColour, roleInfo.image);
    }

    public void SetRoleVisual(string roleName, string displayName, Color roleColor, Color textColor, Sprite image)
    {
        print("Set Role to " + roleName);

        roleText.text = displayName;
        roleText.color = roleColor;

        roleTextback.text = roleText.text;
        roleTextback.color = textColor;

        roleImage.sprite = image;

        countdownText.text = "";
        //
        Player.player.CmdRole(roleName);
    }
    ///
    // Tab Menu
    //
    public void TabMenuClear()
    {
        tabMenu.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, -80, 0);
        tabMenu.GetComponent<RectTransform>().sizeDelta = new Vector3(tabMenuWidth, 160);
        tabMenu.SetActive(false);

        foreach (TabInfo t in tabItemsInfo)
        {
            Destroy(t.obj);
        }
        tabItemsInfo.Clear();
    }
    public void TabMenuAdd(string playerName)
    {
        GameObject newItem = Instantiate(tabItemAsset);
        newItem.name = playerName;
        newItem.transform.parent = tabMenu.transform;
        
        string currentRole = "";
        float yPosition = 0;
        for (int i = 0; i < ids.Count; i++)
        {
            if (ids[i] == playerName)
            {
                currentRole = roles[i];
                yPosition = i * 20;
            }
        }
        RoleInfo roleInfo = ReferenceInfo.referenceInfo.RoleInformation(currentRole);

        newItem.SetActive(true);

        TabInfo tabInfo = new TabInfo();
        tabInfo.playerName = playerName;
        tabInfo.roleName = roleImage.name;
        tabInfo.status = "";
        tabInfo.obj = newItem;
        tabItemsInfo.Add(tabInfo);
        
        newItem.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, -10 - yPosition, 0);
        newItem.transform.Find("NameText").GetComponent<Text>().text = playerName;
        newItem.transform.Find("StatusText").GetComponent<Text>().text = "";
        newItem.transform.Find("RoleImage").GetComponent<Image>().sprite = roleInfo.imageGlow;
        newItem.transform.Find("RoleImage").gameObject.SetActive(false);
        newItem.transform.localScale = new Vector3(1,1,1);

        CheckRoleVisibility(playerName, currentRole, newItem, "");
    }
    public void TabMenuEdit(string playerName, string status, bool showRole)
    {
        // Get Item if not got
        TabInfo newItemInfo = null;

        foreach (TabInfo t in tabItemsInfo)
        {
            if (t.playerName == playerName)
            {
                newItemInfo = t;
            }
        }

        // Edit Status

        newItemInfo.status = status;
        newItemInfo.obj.transform.Find("StatusText").GetComponent<Text>().text = status;

        // Show Role
        if (showRole)
        {
            newItemInfo.obj.transform.Find("RoleImage").gameObject.SetActive(true);
        }
    }
    public void TabMenuRemove(string playerName)
    {
        print("Started removing player " + playerName);
        // Find Player

        for (int i = 0; i < tabItemsInfo.Count; i++)
        {
            print(tabItemsInfo[i].playerName + " " + playerName);

            if (tabItemsInfo[i].playerName == playerName)
            {
                print("Removing " + playerName);

                // Remove from Tab menu

                Destroy(tabItemsInfo[i].obj);

                // Remove Traces
                
                tabItemsInfo.RemoveAt(i);

                print("TabItemsInfo now only has a size of " + tabItemsInfo.Count);

                // End
                i = tabItemsInfo.Count;
            }
        }

        // Resort
        for (int i = 0; i < tabItemsInfo.Count; i++)
        {
            tabItemsInfo[i].obj.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, -10 - i*20, 0);
        }
    }
    public void CheckRoleVisibility (string targetPlayerName, string targetRoleName, GameObject newItem, string forcedRole)
    {
        // Get Item if not got

        if (newItem == null)
        {
            foreach(TabInfo t in tabItemsInfo)
            {
                if (t.playerName == targetPlayerName)
                {
                    newItem = t.obj;
                }
            }
        }

        if (newItem != null)
        {
            // Change Role if Forced

            // Check
            if (targetRoleName != "" && targetRoleName != null)
            {
                if (targetPlayerName == Player.player.playerName)
                {
                    newItem.transform.Find("RoleImage").gameObject.SetActive(true);

                    if (forcedRole != "")
                    {
                        // Change Tab Role Icon to Forced Role
                        RoleInfo roleInfo = ReferenceInfo.referenceInfo.RoleInformation(forcedRole);

                        newItem.transform.Find("RoleImage").GetComponent<Image>().sprite = roleInfo.imageGlow;
                    }
                }
                else
                {
                    RoleInfo roleInfo = null;
                    // Get Role Info
                    if (forcedRole != "")
                    {
                        roleInfo = ReferenceInfo.referenceInfo.RoleInformation(forcedRole);
                    }
                    else
                    {
                        for (int i = 0; i < roles.Count; i++)
                        {
                            if (ids[i] == Player.player.playerName)
                            {
                                roleInfo = ReferenceInfo.referenceInfo.RoleInformation(roles[i]);
                                // End
                                i = roles.Count;
                            }
                        }
                    }
                    // Get Viewed Role Info
                    RoleInfo viewedRoleInfo = ReferenceInfo.referenceInfo.RoleInformation(targetRoleName);

                    newItem.transform.Find("RoleImage").gameObject.SetActive(false);

                    // Determin whether you can see target role based on player role
                    if (roleInfo.name != "amnesiac" && viewedRoleInfo.name == "detective")
                    {
                        newItem.transform.Find("RoleImage").gameObject.SetActive(true);
                    }
                    else if (roleInfo.roleWinType == "traitor" && viewedRoleInfo.roleWinType == "traitor")
                    {
                        newItem.transform.Find("RoleImage").gameObject.SetActive(true);
                    }
                    else if (roleInfo.roleWinType != "innocent" && viewedRoleInfo.name == "jester")
                    {
                        newItem.transform.Find("RoleImage").gameObject.SetActive(true);
                    }
                    else
                    {
                        newItem.transform.Find("RoleImage").gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                // Reset Role Visibility to Pre-round visuals

                newItem.transform.Find("RoleImage").gameObject.SetActive(false);
                newItem.transform.Find("StatusText").GetComponent<Text>().text = "";
            }
        }
    }
    //
    public void TabMenuEnabled (bool value)
    {
        tabMenu.SetActive(value);
    }

    /// <summary>
    /// Shop
    /// </summary>
    
    public void CloseShop()
    {
        Player.player.GetComponent<PlayerShooting>().Shop();
    }
    public void DrawMenu()
    {
        // Remove Items
        GameObject[] removeUs = new GameObject[icons.Count];

        for (int i = 0; i < icons.Count; i++)
        {
            removeUs[i] = icons[i];
        }
        foreach (GameObject icon in removeUs)
        {
            Destroy(icon);
        }

        icons.Clear();

        // Spawn in all Icons
        if (selectedShop.shopName != null && selectedShop.shopName != "")
        {

            for (int i = 0; i < Category().Count; i++)
            {
                // Spawn Shop Object and it's Essential Values
                GameObject newOjbect = Instantiate(shopIcon, IconHolder.transform);
                ShopObject shopObj = newOjbect.GetComponent<ShopObject>();
                
                shopObj.shopObject = Category()[i];
                shopObj.index = i;
                icons.Add(newOjbect);

                /// 
                // Visuals
                //

                Image img = shopObj.GetComponent<Image>();
                shopObj.itemImage.sprite = shopObj.shopObject.image;

                if (shopObj.shopObject.roleItemType == "all")
                {
                    if (shopObj.shopObject.worth == 2)
                    {
                        img.color = new Color(0.75f, 0.75f, 0.75f, 1);
                    }

                    if (shopObj.shopObject.worth == 3)
                    {
                        img.color = new Color(1, 1, 1, 1);
                    }
                }
                else
                {
                    if (shopObj.shopObject.roleItemType == "traitor")
                    {
                        if (shopObj.shopObject.worth == 1)
                        {
                            img.color = new Color(0.5f, 0.25f, 0.25f, 1);
                        }
                        if (shopObj.shopObject.worth == 2)
                        {
                            img.color = new Color(0.5f, 0, 0, 1);
                        }
                        if (shopObj.shopObject.worth == 3)
                        {
                            img.color = new Color(1, 0, 0, 1);
                        }
                    }
                    if (shopObj.shopObject.roleItemType == "detective")
                    {
                        if (shopObj.shopObject.worth == 1)
                        {
                            img.color = new Color(0.25f, 0.25f, 0.5f, 1);
                        }
                        if (shopObj.shopObject.worth == 2)
                        {
                            img.color = new Color(0, 0, 0.5f, 1);
                        }
                        if (shopObj.shopObject.worth == 3)
                        {
                            img.color = new Color(0, 0, 1, 1);
                        }
                    }
                }

                // Can't afford

                if (shopObj.shopObject.worth > Player.player.inventory.crystals)
                {
                    img.color = new Color(img.color.r, img.color.g, img.color.b, 0.1f);
                    shopObj.itemImage.color = new Color(img.color.r, img.color.g, img.color.b, 0.1f);
                }

                // On Special or Favourited

                shopObj.favouriteImage.gameObject.SetActive(false);
                shopObj.onSpecialImage.gameObject.SetActive(false);
            }

            SelectShopItem(0, false);
        }
    }
    public void ToggleShop(string value)
    {
        // Run if is not new info
        if (shopType != value)
        {
            // Set Shop type and activate panel accordingly
            shopType = value;
            shopObject.SetActive(shopType != null && shopType != "");

            // Activate shop info
            if (shopType != null && shopType != "")
            {
                Color color = new Color(0.5f, 0.5f, 0.5f, 1);
                // Find Color based on given role name
                color = ReferenceInfo.referenceInfo.RoleInformation(shopType).roleColour;

                // Set Panel Color

                shopPanel.GetComponent<Image>().color = color;
                shopPanel.GetComponent<Image>().color = new Color(shopPanel.GetComponent<Image>().color.r / 4, shopPanel.GetComponent<Image>().color.g / 4, shopPanel.GetComponent<Image>().color.b / 4, 1);

                // Set All Colored Shop Items to correct color
                float a = 0;
                foreach (Image i in coloredShopImages)
                {
                    a = i.color.a;
                    i.color = color;
                    i.color = new Color(i.color.r, i.color.g, i.color.b, a);
                }
            }
            else
            {

            }
        }
    }

    public void SetShopType(string type)
    {
        selectedShop = new ShopContents();

        // Putting items into shop
        currentCategory = "all";
        currentSortMethod = 0;
        for (int i = 0; i < sortMethodDirection.Length; i++)
        {
            sortMethodDirection[i] = 0;
        }

        if (type != null && type != "")
        {
            selectedShop.shopName = type;

            for (int i = 0; i < allShopItems.Length; i++)
            {
                if (allShopItems[i].roleItemType == type || allShopItems[i].roleItemType == "all")
                {
                    // Adding to correct categories
                    selectedShop.allItems.Add(allShopItems[i]);

                    if (allShopItems[i].itemCategoryType == "weapon")
                    {
                        selectedShop.weaponsItems.Add(allShopItems[i]);
                    }
                    if (allShopItems[i].itemCategoryType == "support")
                    {
                        selectedShop.supportItems.Add(allShopItems[i]);
                    }
                    if (allShopItems[i].itemCategoryType == "misc")
                    {
                        selectedShop.miscItems.Add(allShopItems[i]);
                    }
                }
            }
        }

        DrawMenu();
    }

    public void PurchaseCurrentItem()
    {
        PurchaseItem(lastShopIndex);
    }

    public void SelectShopItem(int index, bool quickSelect)
    {
        if (Category().Count > 0)
        {
            // Set Visuals
            shopItemNameText.text = Category()[index].displayName;
            shopItemDescriptionText.text = Category()[index].description;
            shopItemWorthText.text = "Purchase: " + Category()[index].worth + " Crystals";


            // Quick Purchase
            if ((Time.time - lastShopClick) < 0.3 && lastShopIndex == index && quickSelect)
            {
                PurchaseItem(index);
            }

            // Debug.Log((Time.time - lastClick) + "" + lastIndex);
            lastShopClick = Time.time;
            lastShopIndex = index;
        }
    }

    void PurchaseItem (int index)
    {
        if (Player.player.inventory.crystals >= Category()[index].worth)
        {
            // Take Away the worth of the item to the player's crystals
            Player.player.inventory.crystals -= Category()[index].worth;
            // Find Empty Slot
            for (int i = 5; i < 10; i++)
            {
                if (Player.player.inventory.items[i].itemName == "empty" || Player.player.inventory.items[i].itemName == "" || Player.player.inventory.items[i].itemName == null)
                {
                    // Give the Item to the player
                    Debug.Log("Empty Slot");
                    Player.player.inventory.NewItem(i, Category()[index].itemName);
                    i = 10;

                    DrawMenu();
                }
            }
        }
    }

    public void SelectCategory(string value)
    {
        bool done = false;
        if (currentCategory != value)
        {
            done = true;
        }

        currentCategory = value;
        if (done)
        {
            DrawMenu();
        }
    }

    public void SelectSortType(int value)
    {
        bool done = false;
        if (currentSortMethod != value)
        {
            done = true;
        }

        currentSortMethod = value;
        if (done)
        {
            DrawMenu();
        }
    }
    public void SetSortDirection(int type)
    {
        sortMethodDirection[type] -= 2*sortMethodDirection[type] +1;
        sortIcons[type].transform.eulerAngles += new Vector3(180,0,0);

        if (currentSortMethod == type)
        {
            DrawMenu();
        }
    }

    public List<ShopItemInfo> Category()
    {
        // Get Category

        List<ShopItemInfo> category = new List<ShopItemInfo>();

        if (currentCategory == "all") { category = selectedShop.allItems; }
        if (currentCategory == "weapons") { category = selectedShop.weaponsItems; }
        if (currentCategory == "support") { category = selectedShop.supportItems; }
        if (currentCategory == "misc") { category = selectedShop.miscItems; }

        // Sort Category

        category.Sort((p1, p2) => p1.displayName.CompareTo(p2.displayName));

        if (currentSortMethod == 0)
        {
            List<ShopItemInfo> one = new List<ShopItemInfo>();
            List<ShopItemInfo> two = new List<ShopItemInfo>();
            List<ShopItemInfo> three = new List<ShopItemInfo>();

            foreach (ShopItemInfo s in category)
            {
                if (s.worth == 1) { one.Add(s); }
                if (s.worth == 2) { two.Add(s); }
                if (s.worth == 3) { three.Add(s); }
            }

            category = new List<ShopItemInfo>();
            if (sortMethodDirection[currentSortMethod] == 0)
            {
                category.AddRange(one);
                category.AddRange(two);
                category.AddRange(three);
            }
            else
            {
                category.AddRange(three);
                category.AddRange(two);
                category.AddRange(one);
            }
        }
        if (currentSortMethod == 1)
        {
            List<ShopItemInfo> fav = new List<ShopItemInfo>();
            List<ShopItemInfo> not = new List<ShopItemInfo>();

            foreach (ShopItemInfo s in category)
            {
                if (s.favourite) { fav.Add(s); }
                else { not.Add(s); }
            }

            category = new List<ShopItemInfo>();
            if (sortMethodDirection[currentSortMethod] == 0)
            {
                category.AddRange(fav);
                category.AddRange(not);
            }
            else
            {
                category.AddRange(not);
                category.AddRange(fav);
            }
        }
        if (currentSortMethod == 2)
        {
            List<ShopItemInfo> spec = new List<ShopItemInfo>();
            List<ShopItemInfo> not = new List<ShopItemInfo>();

            foreach (ShopItemInfo s in category)
            {
                if (s.favourite) { spec.Add(s); }
                else { not.Add(s); }
            }

            category = new List<ShopItemInfo>();
            if (sortMethodDirection[currentSortMethod] == 0)
            {
                category.AddRange(spec);
                category.AddRange(not);
            }
            else
            {
                category.AddRange(not);
                category.AddRange(spec);
            }
        }

        return category;
    }

    /// <summary>
    /// Body Inspection
    /// </summary>

    public void ToggleBodyInspection(string playerName, string inspection)
    {
        print("Opening/Closing");

        // Run if is not new info
        if (bodyName != playerName)
        {
            bodyName = playerName;
            bodyObject.SetActive(bodyName != null && bodyName != "");
            elapsedBodyTime = Time.deltaTime;
            
            if (bodyName != null && bodyName != "")
            {
                bodyNameText.text = bodyName;

                RoleInfo roleInfo = ReferenceInfo.referenceInfo.RoleInformation(inspection);

                bodyRoleImage.sprite = roleInfo.image;
                print("Inspected body of " + roleInfo.name);

                if (inspection == "" || inspection == null)
                {
                    bodyQuestionMark.text = "?";
                }
                else
                {
                    bodyQuestionMark.text = "";
                }
            }
        }
    }
}