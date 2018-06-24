using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using Prototype.NetworkLobby;
using System;

[System.Serializable]
public class ToggleEvent : UnityEvent<bool> { }

public class Player : NetworkBehaviour
{
    public static Player player;

    [SyncVar(hook = "OnNameChanged")] public string playerName;
    [SyncVar(hook = "OnColorChanged")] public Color playerColor;

    [SerializeField] ToggleEvent onToggleShared;
    [SerializeField] ToggleEvent onToggleLocal;
    [SerializeField] ToggleEvent onToggleRemote;

    public static List<Player> players = new List<Player>();
    public static List<Player> clientPlayers = new List<Player>();

    NetworkAnimator anim;
    Rigidbody rigid;

    public bool alive;
    [SyncVar] string role;
    public string clientRole;
    [SyncVar(hook = "OnMaskedChanged")] public bool masked;

    public Transform viewTrasform;

    public SimpleSmoothMouseLook cameraScript;
    public Inventory inventory;
    public PlayerSounds playerSounds;
    public PlayerHealth playerHealth;
    public PlayerShooting playerShooting;
    public PlayerMovement playerMovement;

    public Text nameText;
    public GameObject maskedObject;

    public GameObject body;

    public Vector3[] velocities = new Vector3[20];

    // Debugging
    public int cmds;
    public int rpcs;

    void Start()
    {
        anim = GetComponent<NetworkAnimator>();
        rigid = GetComponent<Rigidbody>();
        inventory = GetComponent<Inventory>();

        EnablePlayer();

        if (isLocalPlayer)
        {
            LobbyTopPanel.CursorLocked("", "clear");
        }
    }

    [ServerCallback]
    void OnEnable()
    {
        if (!players.Contains(this))
        {
            players.Add(this);
            NetworkGameInfo.players = players;
        }
    }

    void Update()
    {
        if (!isLocalPlayer)
        {
            if (maskedObject.activeInHierarchy != masked)
            {
                maskedObject.SetActive(masked);
            }
        }
        if (isLocalPlayer)
        {
            if (player == null)
            {
                player = this;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                PlayerCanvas.canvas.TabMenuEnabled(true);
            }
            if (Input.GetKeyUp(KeyCode.Tab))
            {
                PlayerCanvas.canvas.TabMenuEnabled(false);
            }

            anim.animator.SetFloat("Speed", Input.GetAxis("Vertical"));
            anim.animator.SetFloat("Strafe", Input.GetAxis("Horizontal"));
        }
    }

    void FixedUpdate()
    {
        Velocities();

        if (isServer)
        {
            // Check for left players
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] == null && !NetworkGameInfo.networkGameInfo.playerSlotsRemoving.Contains(i))
                {
                    print("Detected Player Left");

                    NetworkGameInfo.networkGameInfo.LeftMemeber();
                    // players.RemoveAt(i);
                    List<Player> p = new List<Player>(players.Count-1);
                    for (int a = 0; a < p.Count; a++)
                    {
                        if (a != i)
                        {
                            p.Add(players[a]);
                        }
                        players = p;
                    }
                    // End
                    i = players.Count;
                }
            }

            if (clientPlayers.Count != players.Count)
            {
                foreach (Player p in players)
                {
                    p.RpcSyncPlayers();
                }
            }
        }
        if (clientRole != role)
        {
            clientRole = role;

            // Get Role Item
            if (isLocalPlayer)
            {
                if (clientRole == "traitor")
                {
                    inventory.NewItem(4, "traitorShop");
                    inventory.crystals = 3;

                    PlayerCanvas.canvas.SetShopType("traitor");
                }
                if (clientRole == "detective")
                {
                    inventory.NewItem(4, "detectiveShop");
                    inventory.crystals = 2;

                    PlayerCanvas.canvas.SetShopType("detective");
                }
                if (clientRole == "serial_killer")
                {
                    inventory.NewItem(4, "bloodyKnife");
                }

                if (clientRole == "" || clientRole == null)
                {
                    PlayerCanvas.canvas.SetShopType("");
                }
            }
        }

        if (isLocalPlayer)
        {
            Effects();

            if (cameraScript.enabled == Cursor.visible)
            {
                cameraScript.enabled = !Cursor.visible;
            }

            if (clientPlayers.Count == 0)
            {
                SyncPlayers();
            }
        }
    }

    
    void Effects ()
    {
        bool foundSmoke = false;
        GameObject[] entities = GameObject.FindGameObjectsWithTag("Entity");
        foreach (GameObject g in entities)
        {
            if (g.name == "Smoke Effect")
            {
                if (Vector3.Distance(playerShooting.cameras[0].transform.position, g.transform.position) < 5)
                {
                    foundSmoke = true;
                }
            }
        }
        if (foundSmoke)
        {
            if (!PlayerCanvas.canvas.smoked)
            {
                PlayerCanvas.canvas.smoked = true;
            }
        }
        else if (PlayerCanvas.canvas.smoked)
        {
            PlayerCanvas.canvas.smoked =false;
        }
    }

    void Velocities ()
    {
        // Move back velocities
        for (int i = 1; i < velocities.Length; i++)
        {
            // Only do so if not already
            if (velocities[i] != velocities[i - 1])
            {
                velocities[i] = velocities[i - 1];
            }
        }
        // Set current velocity
        velocities[0] = rigid.velocity;
    }

    [Command]
    public void CmdRole(string value)
    {
        cmds++;

        role = value;
    }

    [Command]
    public void CmdSendDamage(string playerId, int damage, int direction, string hitter)
    {
        cmds++;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].playerName == playerId && players[i].alive)
            {
                players[i].playerHealth.TakeDamage(damage, direction, hitter);
            }
        }
    }

    [Command]
    public void CmdGotKilled(string killer)
    {
        cmds++;

        if (role == "jester" && killer != playerName && (killer != null && killer != ""))
        {
            NetworkGameInfo.networkGameInfo.JesterWin(playerName);
        }
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].playerName == killer && (killer != null && killer != "") && NetworkGameInfo.networkGameInfo.gameOn)
            {
                print(players[i].playerName + " == " + killer);
                print(NetworkGameInfo.networkGameInfo.roles[i]);
                print(NetworkGameInfo.networkGameInfo.playerIds[i]);

                // Change Role To Amnesiac
                if (NetworkGameInfo.networkGameInfo.roles[i] == "amnesiac")
                {
                    print(killer + " will now try to become a " + role);
                    players[i].ChangeRole(role);
                    players[i].RpcChangeRoleVisual(role);
                }
            }
        }

        // Karma
    }

    [Server]
    void ChangeRole(string newRole)
    {
        // Add Role Count
        NetworkGameInfo.networkGameInfo.RoleCountByRole(newRole, 1, false);
        // Remove Role Count
        NetworkGameInfo.networkGameInfo.RoleCountByRole(role, -1, false);
        // Set Role in roles list
        NetworkGameInfo.networkGameInfo.SetRoleForPlayer(playerName, newRole);
    }

    [ClientRpc]
    void RpcChangeRoleVisual(string newRole)
    {
        rpcs++;

        print(playerName + " is trying to change role to " + newRole);

        if (isLocalPlayer)
        {
            RoleInfo roleInfo = ReferenceInfo.referenceInfo.RoleInformation(newRole);

            print(playerName + ", (me) is trying to change role to " + newRole);
            PlayerCanvas.canvas.SetRoleVisual(roleInfo.name, roleInfo.displayedName, roleInfo.roleColour, roleInfo.textColour, roleInfo.image);

            foreach (Player p in clientPlayers)
            {
                PlayerCanvas.canvas.CheckRoleVisibility(p.playerName, p.clientRole, null, newRole);
            }
        }
    }

    [ClientRpc]
    public void RpcStatus(string id, string status, bool showRole)
    {
        rpcs++;

        PlayerCanvas.canvas.TabMenuEdit(id, status, showRole);
    }

    [ClientRpc]
    public void RpcPickUp(int pickUpId, string itemName, string itemType)
    {
        rpcs++;

        inventory.CollectNewItem(pickUpId, itemName, itemType);
    }
    [Command]
    public void CmdRequestPickupDestroy(int pickUpId)
    {
        cmds++;

        for (int i = 0; i < PickUp.pickUps.Count; i++)
        {
            if (PickUp.pickUps[i].GetComponent<PickUp>().id == pickUpId)
            {
                DestroyPickup(PickUp.pickUps[i]);
                PickUp.pickUps[i].GetComponent<PickUp>().RpcDisable();
                PickUp.pickUps.RemoveAt(i);
            }
        }
    }

    void DestroyPickup(GameObject obj)
    {
        NetworkServer.Destroy(obj);
    }




    void DisablePlayer()
    {
        onToggleShared.Invoke(false);
        rigid.isKinematic = true;

        if (isLocalPlayer)
        {
            SpectatorControl.camera.SetActive(true);
            SpectatorControl.teleport = viewTrasform;

            if (masked)
            {
                CmdToggleMasked(false);
            }

            PlayerCanvas.canvas.View("", -1, false);
            PlayerCanvas.canvas.ScopeImage(false);
            
            foreach (Player p in clientPlayers)
            {
                if (p.alive && p.nameText != null)
                {
                    p.nameText.gameObject.SetActive(true);
                }
            }

            inventory.ResetInv();

            onToggleLocal.Invoke(false);
        }
        else
        {
            onToggleRemote.Invoke(false);
        }
        
        nameText.gameObject.SetActive(false);

        alive = false;
    }

    void EnablePlayer()
    {
        if (isServer)
        {
            playerHealth.immuneTime = 200;
        }

        onToggleShared.Invoke(true);
        rigid.isKinematic = false;

        if (isLocalPlayer)
        {
            SpectatorControl.camera.SetActive(false);

            LobbyTopPanel.CursorLocked("deathNote", "remove");

            onToggleLocal.Invoke(true);
            
            foreach (Player p in players)
            {
                p.nameText.gameObject.SetActive(false);
            }
        }
            
        else
        {
            onToggleRemote.Invoke(true);
        }

        alive = true;
    }

    public void Die()
    {
        if (isLocalPlayer)
        {
            if (NetworkGameInfo.networkGameInfo.gameOn)
            {
                CmdSendLiveState(playerName, -1);
            }

            inventory.Die();
            CmdRequestBodyDrop(transform.position, transform.rotation, playerName);
        }
        if (playerControllerId == -1)
        {
            anim.SetTrigger("Died");
        }

        // Play Death Sound

        Vector3 pos = new Vector3();
        Vector2 volume = new Vector2(0.3f, 0.5f);
        Vector2 pitch = new Vector2(0.8f, 1.2f);

        player.playerSounds.PlaySound("death", pos, volume, pitch, 20, false);

        DisablePlayer();

        // Invoke ("Respawn", respawnTime);
    }

    public void AthorityCall(NetworkBehaviour script, string functionName, float time)
    {
        script.Invoke(functionName, time);
    }

    public void ToggleMasked()
    {
        print("ToggledMasked");

        PlayMaskSound(!masked);

        CmdToggleMasked(true);
    }

    [Command]
    public void CmdToggleMasked(bool sound)
    {
        cmds++;

        masked = !masked;

        if (sound)
        {
            RpcPlayMaskSound(masked);
        }
    }

    [ClientRpc]
    public void RpcPlayMaskSound(bool value)
    {
        rpcs++;

        if (!isLocalPlayer)
        {
            PlayMaskSound(value);
        }
    }

    [ClientRpc]
    public void RpcSyncPlayers()
    {
        rpcs++;

        SyncPlayers();
    }
    void SyncPlayers()
    {
        clientPlayers.Clear();

        GameObject[] newPlayers = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject g in newPlayers)
        {
            clientPlayers.Add(g.GetComponent<Player>());
        }
    }

    void PlayMaskSound(bool value)
    {
        Vector3 pos = new Vector3();
        Vector2 volume = new Vector2(0.5f, 0.8f);
        Vector2 pitch = new Vector2(0.9f, 1.1f);

        if (!value)
        {
            playerSounds.PlaySound("zipDown", pos, volume, pitch, 20, false);
        }
        else
        {
            playerSounds.PlaySound("zipUp", pos, volume, pitch, 20, false);
        }
    }

    [Command]
    public void CmdRequestBodyDrop (Vector3 pos, Quaternion rot, string name)
    {
        cmds++;

        BodyDrop(pos, rot, name);
    }

    [Server]
    public void BodyDrop (Vector3 pos, Quaternion rot, string name)
    {
        // Create Body
        GameObject obj = Instantiate(body, pos + new Vector3(0,0.1f,0), rot);

        // Get Last Highest Velocity

        Vector3 highestVelocity = Vector3.zero;
        for (int i = 0; i < velocities.Length; i++)
        {
            if (velocities[i].magnitude > highestVelocity.magnitude)
            {
                highestVelocity = velocities[i];
            }
        }
        obj.GetComponent<Rigidbody>().velocity = highestVelocity;

        // Set Body Name
        obj.gameObject.name = name;

        NetworkServer.Spawn(obj);

        obj.GetComponent<Body>().playerName = playerName;

        NetworkGameInfo.bodies.Add(obj);
    }

    [Command]
    void CmdSendLiveState(string name, int count)
    {
        cmds++;

        NetworkGameInfo.networkGameInfo.UpdateRoleCount(name, count);
    }

    [ClientRpc]
    public void RpcRespawn()
    {
        rpcs++;

        if (isLocalPlayer || playerControllerId == -1)
            anim.SetTrigger("Restart");

        if (isLocalPlayer)
        {
            Transform spawn = NetworkManager.singleton.GetStartPosition();
            transform.position = spawn.position;
            transform.rotation = spawn.rotation;
        }

        GetComponent<Rigidbody>().velocity = Vector3.zero;

        EnablePlayer();
    }

    void OnNameChanged(string value)
    {
        playerName = value;
        gameObject.name = playerName;
        nameText.text = playerName;

        if (NetworkGameInfo.networkGameInfo == null)
        {
            NetworkGameInfo.playerIdsToAdd.Add(playerName);
        }
        else
        {
            NetworkGameInfo.networkGameInfo.playerIds.Add(playerName);
        }
        print("Alocating as " + playerName);
    }

    void OnColorChanged(Color value)
    {
        playerColor = value;
        // nameText.color = (playerColor);
    }

    void OnMaskedChanged(bool value)
    {
        masked = value;
    }

    [ClientRpc]
    public void RpcRoundOver(string roleName, string[] winners)
    {
        rpcs++;

        if (alive)
        {
            DisablePlayer();
        }

        if (isLocalPlayer)
        {
            PlayerCanvas.canvas.GameWinMenu(roleName, winners);

            LobbyTopPanel.CursorLocked("deathNote", "add");
        }

        // PlayerCanvas.canvas.TabMenuClear();
        for (int i = 0; i < NetworkGameInfo.networkGameInfo.playerIds.Count; i++)
        {
            PlayerCanvas.canvas.CheckRoleVisibility(NetworkGameInfo.networkGameInfo.playerIds[i], "", null, "");
        }

        Invoke("RpcRespawn", 4);
    }

    void BackToLobby()
    {
        FindObjectOfType<NetworkLobbyManager>().SendReturnToLobby();
    }

    [ClientRpc]
    public void RpcRequestCallback(string callback, int time)
    {
        rpcs++;

        if (isLocalPlayer)
        {
            NetworkGameInfo.networkGameInfo.Callback(callback, time);
        }
    }
}