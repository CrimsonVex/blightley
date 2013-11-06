using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    //void Awake() { Time.timeScale = 0.01f; }

    public void StartServer()
    {
        Network.InitializeServer(32, 25000, false);
        Network.sendRate = 15;
        Debug.Log("Server initialized, sendRate: " + Network.sendRate);
        NetworkManager.instance.timer = Time.time;

        NewTimedMessage("Connected as SERVER", 6);
        NetworkManager.instance.serverStarted = true;
    }

    public NetworkConnectionError ConnectToServer(int t)
    {
        NetworkManager.instance.playerJoinType = t;
        return Network.Connect("127.0.0.1", 25000, "");
    }

    public void OnFailedToConnect(NetworkConnectionError error)
    {
        //NewTimedMessage("Couldn't find a server to connect to. Have you created one?", 5);
	}

    public void OnConnectedToServer()
    {

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
        Network.RemoveRPCs(p);
        Network.DestroyPlayerObjects(p);
    }

    public void OnGUI()
    {
        if (!Network.isClient && !Network.isServer)
        {
            if (GUI.Button(new Rect(10, 10, 100, 20), "Start Server"))
                StartServer();

            if (GUI.Button(new Rect(120, 10, 150, 20), "Join Existing Game"))
                NetworkManager.instance.displayTypeChoice = true;

            if (NetworkManager.instance.displayTypeChoice)
            {
                GUI.Label(new Rect(280, 5, 200, 20), "Select player type:");
                if (GUI.Button(new Rect(300, 30, 150, 20), "Mutant"))
                    ConnectToServer(1);
                if (GUI.Button(new Rect(300, 55, 150, 20), "Hero"))
                    ConnectToServer(2);
            }
        }

        if (Network.isServer)
        {
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
                    + "    Position: " + NetworkManager.instance.NPCS[i].target.ToString()
                    );
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
        // Needs fixing
        return new Vector3(Random.Range(-15, 15), 0.66f, Random.Range(-15, 15));
    }

    [RPC]
    public void NewTimedMessage(string m, float l)
    {
        GameObject timedMessage = GameObject.Instantiate(timedMessagePrefab) as GameObject;
        timedMessage.GetComponent<TimedMessage>().text = m;
        timedMessage.GetComponent<TimedMessage>().lifetime = l;
    }
}
