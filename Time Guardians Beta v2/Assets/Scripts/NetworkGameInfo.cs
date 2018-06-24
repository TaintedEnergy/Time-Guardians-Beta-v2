using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System.Collections.Generic;

public class NetworkGameInfo : NetworkBehaviour
{
    public static NetworkGameInfo networkGameInfo;

    [SyncVar(hook = "OnGameOnChange")] public bool gameOn;

    [SyncVar(hook = "OnPlayerIdsChange")] public SyncListString playerIds = new SyncListString();
    [SyncVar(hook = "OnRolesChange")] public SyncListString roles = new SyncListString();

    [SyncVar(hook = "OnInnocentCountChange")] public int innocentAlive;
    [SyncVar(hook = "OnTraitorCountChange")] public int traitorAlive;
    [SyncVar(hook = "OnSoloCountChange")] public int soloAlive;

    public static List<Player> players = new List<Player>();

    public static List<GameObject> bodies = new List<GameObject>();

    public static List<GameObject> pickUpSpawnpoints = new List<GameObject>();
    [SerializeField] GameObject pickUp;

    public static List<string> playerIdsToAdd = new List<string>();
    List<string> playerIdsToRemove = new List<string>();
    public List<int> playerSlotsRemoving = new List<int>();

    private void Awake()
    {
        networkGameInfo = this;
        
        bodies.Clear();
    }

    void Start()
    {
        Invoke("GetNewRoles", ReferenceInfo.referenceInfo.initialDelay);

        // Placing Pickups

        ReplaceEntities();

        if (isServer)
        {
            PlayerCanvas.canvas.countdownTime = ReferenceInfo.referenceInfo.initialDelay + 1;
            PlayerCanvas.canvas.Countdown();
        }

        for (int i = 0; i < playerIdsToAdd.Count; i++)
        {
            playerIds.Add(playerIdsToAdd[i]);
            print("Added " + playerIdsToAdd[i]);
        }
        playerIdsToAdd.Clear();
    }

    [ServerCallback]
    void GetNewRoles()
    {
        // roles = new SyncListString();
        RemoveBodies();
        playerIdsToRemove.Clear();
        playerSlotsRemoving.Clear();

        SetUp();

        for (int i = 0; i < players.Count; i++)
        {
            players[i].RpcRequestCallback("DisplayInfo", 1);
        }
    }

    [Server]
    public void SetUp()
    {
        // RESETING
        innocentAlive = 0;
        traitorAlive = 0;
        soloAlive = 0;

        // Assuring working role counts
        string[] roundRoleOrder = new string[playerIds.Count];

        for (int i = 0; i < roundRoleOrder.Length; i++)
        {
            int index = Random.Range(0, ReferenceInfo.referenceInfo.roleListOrder[i].stringArray.Length);
            roundRoleOrder[i] = ReferenceInfo.referenceInfo.roleListOrder[i].stringArray[index];
        }
        // Shuffling
        for (int i = 0; i < roundRoleOrder.Length; i++)
        {
            string temp = roundRoleOrder[i];
            int randomIndex = Random.Range(0, roundRoleOrder.Length);
            roundRoleOrder[i] = roundRoleOrder[randomIndex];
            roundRoleOrder[randomIndex] = temp;
        }
        // Assigning Final Roles

        for (int i = 0; i < playerIds.Count; i++)
        {
            roles.Add(roundRoleOrder[i]);
            
            // Finding Win Type
            RoleInfo roleInfo = ReferenceInfo.referenceInfo.RoleInformation(roundRoleOrder[i]);

            // Adding count by team

            if (roleInfo.roleWinType == "innocent")
            {
                innocentAlive++;
            }
            if (roleInfo.roleWinType == "traitor")
            {
                traitorAlive++;
            }
            if (roleInfo.roleWinType == "solo")
            {
                soloAlive++;
            }
        }

        // Respawning Stupid-Dead Players

        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].alive)
            {
                players[i].RpcRespawn();
            }
            players[i].playerHealth.health = 100;
        }

        // Game On
        gameOn = true;
        print("Nigger");
    }

    [Server]
    public void LeftMemeber()
    {
        int foundAt = -1;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == null && !playerSlotsRemoving.Contains(i))
            {
                foundAt = i;
                i = players.Count;
            }
        }
        // If found
        if (foundAt != -1)
        {
            print("Found and removed player");

            if (roles.Count >= foundAt+1)
            {
                RoleCountByRole(roles[foundAt], -1, true);
            }

            // Remove Player from Players List
            RpcLeftMember(playerIds[foundAt]);

            players.RemoveAt(foundAt);
            playerIdsToRemove.Add(playerIds[foundAt]);
            playerSlotsRemoving.Add(foundAt);
        }
    }

    [ClientRpc]
    void RpcLeftMember (string playerName)
    {
        if (Player.player != null)
        {
            Player.player.rpcs++;
        }

        PlayerCanvas.canvas.TabMenuRemove(playerName);
    }

    [ServerCallback]
    public void UpdateRoleCount(string playerName, int count)
    {
        // If game still on
        if (!gameOn)
        {
            return;
        }

        // Finding Role Info
        RoleInfo roleInfo = null;

        for (int i = 0; i < playerIds.Count; i++)
        {
            if (playerIds[i] == playerName)
            {
                roleInfo = ReferenceInfo.referenceInfo.RoleInformation(roles[i]);
            }
        }

        // Finding Win Type

        // Chaning Team count by win type
        if (roleInfo.roleWinType == "innocent")
        {
            innocentAlive += count;
        }
        if (roleInfo.roleWinType == "traitor")
        {
            traitorAlive += count;
        }
        if (roleInfo.roleWinType == "solo")
        {
            soloAlive += count;
        }

        // Detect and Manage wins

        print(innocentAlive + " " + traitorAlive + " " + soloAlive + " ");

        Invoke("DetectWin", 1f);
    }

    [Server]
    void DetectWin()
    {
        // If game still on
        if (!gameOn)
        {
            return;
        }

        // Role Type
        string won = "";
        string soloWinner = "";

        if (innocentAlive == 0 && soloAlive == 0)
        {
            won = "traitor";
            // Traitors Win
        }
        else if (traitorAlive == 0 && soloAlive == 0)
        {
            won = "innocent";
            // Innocents Win
        }
        else if (innocentAlive == 0 && traitorAlive == 0 && soloAlive == 1)
        {
            // Find last alive player
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].alive)
                {
                    won = roles[i];
                    soloWinner = players[i].playerName;
                }
            }

            // Solo Win
        }

        string[] winners = null;
        // Get Winners
        if (won != "" && (soloWinner == null || soloWinner == ""))
        {
            List<string> winnerList = new List<string>();

            // Get all players of same RoleType
            for (int i = 0; i < playerIds.Count; i++)
            {
                for (int a = 0; a < ReferenceInfo.referenceInfo.rolesInfo.Length; a++)
                {
                    if (roles[i] == ReferenceInfo.referenceInfo.rolesInfo[a].name && won == ReferenceInfo.referenceInfo.rolesInfo[a].roleWinType)
                    {
                        winnerList.Add(playerIds[i]);
                        // End
                        a = ReferenceInfo.referenceInfo.rolesInfo.Length;
                    }
                }
            }

            // Assign Winners to Winner List
            winners = new string[winnerList.Count];
            for (int i = 0; i < winners.Length; i++)
            {
                winners[i] = winnerList[i];
            }
        }
        if (soloWinner != null && soloWinner != "")
        {
            winners = new string[1];
            winners[0] = soloWinner;
        }

        ManageWin(won, winners);
    }

    [Server]
    public void JesterWin(string playerName)
    {
        string[] winners = new string[1];
        winners[0] = playerName;

        ManageWin("jester", winners);
    }

    [Server]
    void RemoveBodies()
    {
        for (int i = 0; i < bodies.Count; i++)
        {
            NetworkServer.Destroy(bodies[i]);
        }
        bodies.Clear();
    }

    [Server]
    public void ManageWin(string roleWon, string[] winners)
    {
        if (roleWon != "" && gameOn)
        {
            gameOn = false;
            for (int i = 0; i < players.Count; i++)
            {
                players[i].RpcRoundOver(roleWon, winners);
            }
            roles.Clear();

            Invoke("RemoveBodies", 3f);
            
            GameObject[] entities = GameObject.FindGameObjectsWithTag("Entity");
            
            foreach (GameObject ent in entities)
            {
                if (ent.GetComponent<NetworkIdentity>() != null)
                {
                    NetworkServer.Destroy(ent);
                }
                else
                {
                    for (int i = 0; i < players.Count; i++)
                    {
                        players[i].RpcRequestCallback("ResetEntities", 0);
                    }
                }
            }
            PickUp.pickUps.Clear();

            for (int i = 0; i < players.Count; i++)
            {
                players[i].RpcRequestCallback("RawRoleDisplay", 1);
            }
            Invoke("GetNewRoles", ReferenceInfo.referenceInfo.restartDelay);

            for (int i = 0; i < players.Count; i++)
            {
                players[i].RpcRequestCallback("RawCountdownDisplay", 1);
            }

            // Placing Pickups

            Invoke("ReplaceEntities", 3f);
        }
    }
    
    [Server]
    public void RoleCountByRole(string roleName, int amount, bool detectWin)
    {
        RoleInfo roleInfo = ReferenceInfo.referenceInfo.RoleInformation(roleName);

        if (roleInfo.roleWinType == "innocent")
        {
            RawRoleCount(amount, 0,0);
        }
        if (roleInfo.roleWinType == "traitor")
        {
            RawRoleCount(0, amount, 0);
        }
        if (roleInfo.roleWinType == "solo")
        {
            RawRoleCount(0, 0, amount);
        }

        if (detectWin)
        {
            Invoke("DetectWin", 1f);
        }
    }

    [Server]
    public void SetRoleForPlayer(string playerName, string newRole)
    {
        for (int i = 0; i < playerIds.Count; i++)
        {
            if (playerIds[i] == playerName)
            {
                roles[i] = newRole;
            }
        }
    }

    [Server]
    void RawRoleCount(int innocent, int traitor, int solo)
    {
        innocentAlive += innocent;
        traitorAlive += traitor;
        soloAlive += solo;
    }

    void ReplaceEntities()
    {
        foreach (GameObject g in pickUpSpawnpoints)
        {
            GameObject newPickup = Instantiate(pickUp, g.transform.position, g.transform.rotation);
            NetworkServer.Spawn(newPickup);
        }
    }

    public void Callback(string callback, int time)
    {
        Invoke(callback, time);
    }

    /// <summary>
    /// RPC's
    /// </summary>

    void RawRoleDisplay ()
    {
        PlayerCanvas.canvas.ResetRoleVisuals();
    }
    void RawCountdownDisplay()
    {
        PlayerCanvas.canvas.countdownTime = ReferenceInfo.referenceInfo.restartDelay;
        PlayerCanvas.canvas.Countdown();
    }

    void DisplayInfo()
    {
        PlayerCanvas.canvas.SyncData(roles, playerIds);
        for (int i = 0; i < playerIds.Count; i++)
        {
            PlayerCanvas.canvas.TabMenuAdd(playerIds[i]);
        }
    }

    void ResetEntities ()
    {
        GameObject[] entities = GameObject.FindGameObjectsWithTag("Entity");

        foreach (GameObject ent in entities)
        {
            Destroy(ent);
        }
    }

    /// <summary>
    /// Hooks
    /// </summary>

    void OnPlayerIdsChange(SyncListString value)
    {
        playerIds = value;
    }

    void OnGameOnChange(bool value)
    {
        gameOn = value;
    }

    void OnRolesChange(SyncListString value)
    {
        roles = value;
    }

    void OnInnocentCountChange(int value)
    {
        innocentAlive = value;
    }
    void OnTraitorCountChange(int value)
    {
        traitorAlive = value;
    }
    void OnSoloCountChange(int value)
    {
        soloAlive = value;
    }
}