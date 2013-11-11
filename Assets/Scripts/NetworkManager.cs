using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;

public class NetworkManager : MonoBehaviour 
{
    // MonoBehaviour Singleton Begin
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
    // Monobehaviour Singleton End


    public struct PLAYER
    {
        public NetworkPlayer player;
        public int playerID;
        public int playerType;
        public GameObject pObject;
    }

    public struct NPC
    {
        public GameObject npc;
        public int npcID;
        public int npcType; // 0 is normal, 1 is infected
        public GameObject npcObject;
        public Vector3 target;
    }

    public GameObject playerPrefab, playerCamera, timedMessagePrefab;

    public List<PLAYER> PLAYERS = new List<PLAYER>();
    public List<NPC> NPCS = new List<NPC>();

    public int playerJoinType = -1;
    public bool serverStarted = false;
    
    public int currPlayerCount = 0, totalPlayerCount = 0, npcTotalInfected = 0;
    public double timer = 0;

    private bool displayTypeChoice = false;

    private int remotePort = 25000;
    private float refreshTimerTimeSince = 0f;
    private ConnectionTesterStatus natCapable = ConnectionTesterStatus.Undetermined;
    private bool filterNATHosts = false;
    private string gameName = "NetworkTest00001";
    //void Awake() { Time.timeScale = 0.01f; }

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

    void Update()
    {
        if ((refreshTimerTimeSince += Time.deltaTime) > 1.0f)
        {
            refreshTimerTimeSince = 0f;
        }
    }

    public void StartServer()
    {
        Network.InitializeServer(32, 25002, !Network.HavePublicAddress());
        MasterServer.updateRate = 3;
        MasterServer.RegisterHost(gameName, "Some sort of networking test", "Anday");
        Network.sendRate = 15;
        Debug.Log("Server initialized, sendRate: " + Network.sendRate);
        NetworkManager.instance.timer = Time.time;

        NewTimedMessage("Connected as SERVER", 6);
        NetworkManager.instance.serverStarted = true;
    }

    public void OnDisconnectedFromServer(NetworkDisconnection info)
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
        Debug.Log(log);
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
                if (!(filterNATHosts && data[i].useNat))
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
                            NetworkManager.instance.playerJoinType = 1;
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

        if (Network.isServer)
        {
            GUI.Label(new Rect(Screen.width - 410, Screen.height - 25, 400, 20), "IP: " + Network.player.ipAddress + "     Port: " + Network.player.port);
            GUI.Label(new Rect(400, 10, 200, 20), "Total Player Count: " + NetworkManager.instance.totalPlayerCount.ToString());
            GUI.Label(new Rect(400, 35, 200, 20), "Current Player Count: " + NetworkManager.instance.currPlayerCount.ToString());
            GUI.Label(new Rect(400, 60, 200, 20), "PLAYERS.Count: " + NetworkManager.instance.PLAYERS.Count.ToString());
            GUI.Label(new Rect(400, 85, 200, 20), "PLAYERS Count: " + PLAYERS.Count.ToString());

            for (int i = 0; i < NetworkManager.instance.PLAYERS.Count; i++)
            {
                GUI.Label(new Rect(10, 160 + (i * 20), 300, 20), "Player " + NetworkManager.instance.PLAYERS[i].playerID.ToString() 
                    + "    Type: " + NetworkManager.instance.PLAYERS[i].playerType
                    + "    Position: " + NetworkManager.instance.PLAYERS[i].pObject.transform.position
                    );
            }

            for (int i = 0; i < NetworkManager.instance.NPCS.Count; i++)
            {
                GUI.Label(new Rect(10, 350 + (i * 20), 300, 20), "NPC " + NetworkManager.instance.NPCS[i].npcID.ToString()
                    + "    Type: " + NetworkManager.instance.NPCS[i].npcType.ToString()
                    + "    Position: " + NetworkManager.instance.NPCS[i].npcObject.transform.position.ToString()
                    );
            }
        }

        if (Network.isClient)
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

    public void OnPlayerConnected(NetworkPlayer p)
    {
        NetworkManager.instance.currPlayerCount++;
        NetworkManager.instance.totalPlayerCount++;

        PLAYER newPlayer = new PLAYER();
        newPlayer.player = p;
        newPlayer.playerID = NetworkManager.instance.totalPlayerCount;      // Where we first set the Player's ID

        networkView.RPC("TellClient", p, newPlayer.playerID, p);
        NetworkManager.instance.PLAYERS.Add(newPlayer);
        networkView.RPC("NewTimedMessage", RPCMode.All, "Player " + newPlayer.playerID + " connected", 5.0f);
    }

    [RPC]
    public void TellClient(int id, NetworkPlayer p)
    {
        // This runs on the newly connected client. They're player object is given its ID and type
        GameObject player = Network.Instantiate(playerPrefab, SpawnPosition(), Quaternion.identity, 0) as GameObject;

        player.GetComponent<Player>().player.playerID = id;
        player.GetComponent<Player>().player.playerType = NetworkManager.instance.playerJoinType;

        // Now we need to tell the server which type the player joined as, as only the player knows this right now
        networkView.RPC("TellServer", RPCMode.Server, p, NetworkManager.instance.playerJoinType);
    }

    [RPC]
    public void TellServer(NetworkPlayer p, int type)
    {
        // Find the PLAYER list item using the NetworkPlayer this was called from
        int index = NetworkManager.instance.PLAYERS.FindIndex(a => a.player == p);
        NetworkManager.PLAYER k = new NetworkManager.PLAYER();

        // Keep all the fields the same except the type, which we then set to the type passed from the client
        k.player = NetworkManager.Instance.PLAYERS[index].player;
        k.playerID = NetworkManager.Instance.PLAYERS[index].playerID;
        k.playerType = type;
        k.pObject = NetworkManager.Instance.PLAYERS[index].pObject;

        NetworkManager.instance.PLAYERS[index] = k;
    }

    public void OnPlayerDisconnected(NetworkPlayer p)
    {
        NetworkManager.instance.currPlayerCount--;

        PLAYER deadPlayer = NetworkManager.instance.PLAYERS.Find(PLAYER => PLAYER.player == p);
        Debug.Log("Player " + deadPlayer.playerID + " disconnected...");
        NetworkManager.instance.PLAYERS.RemoveAll(PLAYER => PLAYER.player == p);
        Network.RemoveRPCs(p);
        Network.DestroyPlayerObjects(p);

        networkView.RPC("NewTimedMessage", RPCMode.All, "Player " + deadPlayer.playerID + " disconnected...", 5.0f);
    }

    public Vector3 SpawnPosition()
    {
        Vector3 pos = new Vector3(Random.Range(-40, 40), 0, Random.Range(-40, 40));
        Node node = (Node)AstarPath.active.GetNearest(pos);
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
