using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;

public class NetworkManager : MonoBehaviour 
{
    // MonoBehaviour Singleton Begin -----------------------------------------------------------
    private static NetworkManager instance;

    public NetworkManager()
    {
        if (instance != null)
            return;
        instance = this;
    }

    public static NetworkManager Instance
    {
        get
        {
            if (instance == null)
                instance = (NetworkManager)GameObject.FindObjectOfType(typeof(NetworkManager));

            if (instance == null)
            {
                GameObject gObject = new GameObject("NetworkManager");
                instance = gObject.AddComponent<NetworkManager>();
                DontDestroyOnLoad(gObject);
            }
            return instance;
        }
    }
    // Monobehaviour Singleton End ---------------------------------------------------------------


    public struct PLAYER    // A list the server keeps containing information for each player
    {
        public NetworkPlayer player;    // Store the unity network info for each player
        public int playerID;            // The ID we assign the player e.g. 1, 2, 3.
        public int playerType;          // The type the player is e.g. Mutant, Hero (1, 2)
        public GameObject pObject;      // A reference to the players GameObject which we can use for position, etc.
    }

    public struct NPC       // A list the server keeps for all NPC objects
    {
        public GameObject npcObject;    // The GameObject reference for the NPC
        public int npcID;               // The NPC's ID e.g. 1, 2, 3
        public int npcType;             // 0 is normal, 1 is infected
        public Vector3 target;          // The NPC's current target that it moves towards
    }

    public GameObject playerPrefab, playerCamera, timedMessagePrefab;   // The base objects we instantiate

    public List<PLAYER> PLAYERS = new List<PLAYER>();
    public List<NPC> NPCS = new List<NPC>();

    public int playerJoinType = -1;         // When a player joins, the player type they join as is recorded
    public float timeStarted;               // The game time the server started
    public int currPlayerCount = 0, totalPlayerCount = 0, npcTotalInfected = 0;     // Statistics for player and NPC count

    private bool displayTypeChoice = false;     // Display the player type selection options?
    private ConnectionTesterStatus natCapable = ConnectionTesterStatus.Undetermined;
    private string gameName = "NetworkTest00001";

    void OnFailedToConnectToMasterServer(NetworkConnectionError info)
    {
	    Debug.Log(info);
    }

    void OnFailedToConnect(NetworkConnectionError info)
    {
	    Debug.Log(info);
    }

    void Awake()
    {
        natCapable = Network.TestConnection();

        if (Network.HavePublicAddress())
            Debug.Log("This machine has a public IP address");
        else
            Debug.Log("This machine has a private IP address");
    }

    public void StartServer()
    {
        Network.InitializeServer(32, 25002, !Network.HavePublicAddress());              // Initialize the server
        MasterServer.updateRate = 3;
        MasterServer.RegisterHost(gameName, "Wait this isnt Star Fender", "Mr Booth");  // Register the server with Unity's master serve
        Network.sendRate = 15;
        Debug.Log("Server initialized, sendRate: " + Network.sendRate);
        NetworkManager.instance.timeStarted = Time.time;

        NewTimedMessage("Connected as SERVER", 6);      // The timed message is a visual message all players receive regarding an event
    }

    public void OnDisconnectedFromServer(NetworkDisconnection info)     // If the server is shutdown or goes offline...
    {
        if (Network.isServer)
            Debug.Log("SERVER: You disabled the server");
        else
        {
            if (info == NetworkDisconnection.LostConnection)
                ServerDied("CLIENT: Lost connection to the server");
            else
                ServerDied("CLIENT: Successfully diconnected from the server");
        }
    }

    public void ServerDied(string log)
    {
        // Let's remove the player's objects from their client side and reset some variables
        Debug.Log(log);
        NetworkManager.instance.displayTypeChoice = false;
        NetworkPlayer p = gameObject.networkView.owner;
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject g in players)
            Destroy(g);
        Network.RemoveRPCs(p);
        Network.DestroyPlayerObjects(p);
        MasterServer.ClearHostList();
        MasterServer.RequestHostList(gameName);
    }

    public void OnGUI()
    {
        //GUI.Label(new Rect(200, 50, 200, 20), "DeltaTime: " + Time.deltaTime.ToString());

        if (!Network.isClient && !Network.isServer)
        {
            if (GUI.Button(new Rect(10, 10, 100, 20), "Start Server"))
                StartServer();

            if (GUI.Button(new Rect(110, 10, 130, 20), "Refresh Server List"))
            {
                MasterServer.ClearHostList();
                MasterServer.RequestHostList(gameName);
            }

            HostData[] data = MasterServer.PollHostList();

            for (int i = 0; i < data.Length; i++)
            {
                // Do not display NAT enabled games if we cannot do NAT punchthrough
                if (!data[i].useNat)
                {
                    string name = data[i].gameName + " " + data[i].connectedPlayers + " / " + data[i].playerLimit;
                    string hostInfo;
                    hostInfo = "[";
                    // Here we display all IP addresses, there can be multiple in cases where
                    // internal LAN connections are being attempted. In the GUI we could just display
                    // the first one in order not confuse the end user, but internally Unity will
                    // do a connection check on all IP addresses in the element.ip list, and connect to the
                    // first valid one.
                    for (int j = 0; j < data[i].ip.Length; j++)
                        hostInfo = hostInfo + data[i].ip[j] + ":" + data[i].port + " ";

                    hostInfo = hostInfo + "]   \"" + data[i].gameName + "\"    --Player Count: " + data[i].connectedPlayers + "/" + data[i].playerLimit;

                    if (GUI.Button(new Rect(20, 90, 600, 40), hostInfo.ToString()))
                        NetworkManager.instance.displayTypeChoice = true;

                    if (NetworkManager.instance.displayTypeChoice)
                    {
                        GUI.Label(new Rect(280, 5, 200, 20), "Select player type:");
                        if (GUI.Button(new Rect(300, 30, 150, 20), "Mutant"))
                        {
                            NetworkManager.instance.playerJoinType = 1;     // Make sure we record the type they pick
                            Network.Connect(data[i]);
                        }
                        if (GUI.Button(new Rect(300, 55, 150, 20), "Hero"))
                        {
                            NetworkManager.instance.playerJoinType = 2;
                            Network.Connect(data[i]);
                        }
                    }
                }
            }
        }

        if (Network.isServer)       // The server get's a bunch of information displayed on player and NPC count, positions, etc.
        {
            GUI.Label(new Rect(Screen.width - 410, Screen.height - 25, 400, 20), "IP: " + Network.player.ipAddress + "     Port: " + Network.player.port);
            GUI.Label(new Rect(400, 10, 200, 20), "Total Player Count: " + NetworkManager.instance.totalPlayerCount.ToString());
            GUI.Label(new Rect(400, 35, 200, 20), "Current Player Count: " + NetworkManager.instance.currPlayerCount.ToString());
            GUI.Label(new Rect(400, 60, 200, 20), "PLAYERS.Count: " + NetworkManager.instance.PLAYERS.Count.ToString());

            for (int i = 0; i < NetworkManager.instance.PLAYERS.Count; i++)
            {
                if (NetworkManager.instance.PLAYERS[i].playerID != 0 && NetworkManager.instance.PLAYERS[i].playerType != 0 && NetworkManager.instance.PLAYERS[i].pObject)
                {
                    GUI.Label(new Rect(10, 160 + (i * 20), 300, 20), "Player " + NetworkManager.instance.PLAYERS[i].playerID.ToString()
                        + "    Type: " + NetworkManager.instance.PLAYERS[i].playerType
                        + "    Position: " + NetworkManager.instance.PLAYERS[i].pObject.transform.position
                        );
                }
                else
                    Debug.Log("An Attribute of the PLAYER list is null");
            }

            for (int i = 0; i < NetworkManager.instance.NPCS.Count; i++)
            {
                if (NetworkManager.instance.NPCS[i].npcID != null && NetworkManager.instance.NPCS[i].npcType != null && NetworkManager.instance.NPCS[i].npcObject)
                {
                    GUI.Label(new Rect(10, 350 + (i * 20), 300, 20), "NPC " + NetworkManager.instance.NPCS[i].npcID.ToString()
                        + "    Type: " + NetworkManager.instance.NPCS[i].npcType.ToString()
                        + "    Position: " + NetworkManager.instance.NPCS[i].npcObject.transform.position.ToString()
                        );
                }
                else
                    Debug.Log("An Attribute of the NPC list is null");
            }
        }

        if (Network.isClient)       // The client gets a disconnect button
        {
            if (GUI.Button(new Rect(Screen.width - 410, Screen.height - 25, 200, 20), "Disconnect"))
            {
                Network.Disconnect();
                MasterServer.UnregisterHost();
                MasterServer.ClearHostList();
                MasterServer.RequestHostList(gameName);
            }
        }
    }

    public void OnPlayerConnected(NetworkPlayer p)      // This function is called only on the server once a client connects
    {
        NetworkManager.instance.currPlayerCount++;      // Increase the server's tally of counts
        NetworkManager.instance.totalPlayerCount++;

        PLAYER newPlayer = new PLAYER();                // Create a new PLAYER object to add to the server's list
        newPlayer.player = p;                           // Unity's networkplayer object can be used to identify the player in the list
        newPlayer.playerID = NetworkManager.instance.totalPlayerCount;      // Where we first set the Player's ID number

        networkView.RPC("TellClient", p, newPlayer.playerID, p);    // Call the RPC function for the client
        NetworkManager.instance.PLAYERS.Add(newPlayer);
        networkView.RPC("NewTimedMessage", RPCMode.All, "Player " + newPlayer.playerID + " connected", 5.0f);
    }

    [RPC]
    public void TellClient(int id, NetworkPlayer p)
    {
        // This function runs on the newly connected client. They're player object is given its ID and type
        GameObject player = Network.Instantiate(playerPrefab, SpawnPosition(), Quaternion.identity, 0) as GameObject;

        player.GetComponent<Player>().player.playerID = id;     // The client now knows their own ID number

        // The client's version of NetworkManager has their player type recorded, but the server's version of NetworkManager doesn't.
        player.GetComponent<Player>().player.playerType = NetworkManager.instance.playerJoinType;
        
        // Now we need to tell the server which type the player joined as, as only the player knows this right now
        // because when this script recorded which type button was pressed, the script belonged to neither the 
        // client or the server, so it went from being neither to being a client. Only now will the server know...
        networkView.RPC("TellServer", RPCMode.Server, p, NetworkManager.instance.playerJoinType);
    }

    [RPC]
    public void TellServer(NetworkPlayer p, int type)
    {
        // Find the PLAYER list item using the NetworkPlayer this was called from
        int index = NetworkManager.instance.PLAYERS.FindIndex(a => a.player == p);
        GameObject playerObject = NetworkManager.instance.PLAYERS.Find(a => a.player == p).pObject;
        NetworkManager.PLAYER k = new NetworkManager.PLAYER();

        // Keep all the fields the same except the type, which we then set to the type passed from the client
        k.player = NetworkManager.Instance.PLAYERS[index].player;
        k.playerID = NetworkManager.Instance.PLAYERS[index].playerID;
        k.playerType = type;
        k.pObject = NetworkManager.Instance.PLAYERS[index].pObject;

        NetworkManager.instance.PLAYERS[index] = k;
    }

    public void OnPlayerDisconnected(NetworkPlayer p)   // Called on the server once a client disconnects
    {
        NetworkManager.instance.currPlayerCount--;

        // Find the player that just disconnected in the server's PLAYER list
        PLAYER deadPlayer = NetworkManager.instance.PLAYERS.Find(PLAYER => PLAYER.player == p);
        Debug.Log("Player " + deadPlayer.playerID + " disconnected...");

        // Remove them from the list, remove their RPC function calls and their player objects.
        NetworkManager.instance.PLAYERS.RemoveAll(PLAYER => PLAYER.player == p);
        Network.RemoveRPCs(p);
        Network.DestroyPlayerObjects(p);

        networkView.RPC("NewTimedMessage", RPCMode.All, "Player " + deadPlayer.playerID + " disconnected...", 5.0f);
    }

    public Vector3 SpawnPosition()      // Spawn position for a new player that isn't inside a collider
    {
        // Find the operating area of the A* grid
        GridGraph gg = AstarPath.active.graphs[0] as GridGraph;
        float h = gg.depth / 2;
        float w = gg.width / 2;

        // Use the A* grid size to generate a random world location
        Node node = (Node)AstarPath.active.GetNearest(new Vector3(Random.Range(-h, w), 0, Random.Range(-h, w)));

        // Ensure the node we've picked is walkable by generating new positions of necessary
        while (!node.walkable)
            node = (Node)AstarPath.active.GetNearest(new Vector3(Random.Range(-h, w), 0, Random.Range(-h, w)));

        Vector3 openPoint = (Vector3)node.position;
        return new Vector3(openPoint.x, playerPrefab.transform.position.y, openPoint.z);
    }

    [RPC]
    public void NewTimedMessage(string m, float l)
    {
        GameObject timedMessage = GameObject.Instantiate(timedMessagePrefab) as GameObject;
        timedMessage.GetComponent<TimedMessage>().text = m;
        timedMessage.GetComponent<TimedMessage>().lifetime = l;
    }
}