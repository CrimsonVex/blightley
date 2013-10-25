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

    public static Random rand = new Random();
    public bool serverStarted = false;
    public GameObject playerPrefab, playerCamera, timedMessagePrefab;
    public List<PLAYER> PLAYERS = new List<PLAYER>();
    public int currPlayerCount = 0, totalPlayerCount = 0;
    public double timer = 0;

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

    public NetworkConnectionError ConnectToServer()
    {
        return Network.Connect("127.0.0.1", 25000, "");
    }

    public void OnFailedToConnect(NetworkConnectionError error)
    {
        //NewTimedMessage("Couldn't find a server to connect to. Have you created one?", 5);
	}

    public void Update()
    {

    }

    public void OnGUI()
    {
        if (!Network.isClient && !Network.isServer)
        {
            if (GUI.Button(new Rect(10, 10, 100, 20), "Start Server"))
                StartServer();

            if (GUI.Button(new Rect(120, 10, 150, 20), "Join Existing Game"))
                Debug.Log(ConnectToServer());
        }

        if (Network.isServer)
        {
            GUI.Label(new Rect(Screen.width - 130, 10, 125, 20), "Player Count: " + NetworkManager.instance.currPlayerCount.ToString());
            GUI.Label(new Rect(Screen.width - 150, 40, 125, 20), "PLAYERS Count: " + NetworkManager.instance.PLAYERS.Count.ToString());
            for (int i = 0; i < NetworkManager.instance.currPlayerCount; i++)
            {
                //GUI.Label(new Rect(20, 50 + (i * 20), 200, 20), "Player " + i + " Position: " + PLAYERS[i].playerPos.ToString());
            }
        }
    }

    

    public void OnPlayerConnected(NetworkPlayer p)
    {
        NetworkManager.instance.currPlayerCount++;
        NetworkManager.instance.totalPlayerCount++;

        PLAYER newPlayer = new PLAYER();
        newPlayer.player = p;
        newPlayer.playerID = NetworkManager.instance.totalPlayerCount;
        //newPlayer.pObject = player;

        if (NetworkManager.instance.totalPlayerCount % 2 == 0)
            newPlayer.playerType = 2;
        else
            newPlayer.playerType = 1;

        networkView.RPC("Identification", p, NetworkManager.instance.totalPlayerCount, p, newPlayer.playerType);
        NetworkManager.instance.PLAYERS.Add(newPlayer);
        networkView.RPC("NewTimedMessage", RPCMode.All, "Player " + newPlayer.playerID + " connected", 5.0f);
    }

    [RPC]
    public void Identification(int totalPCount, NetworkPlayer p, int type)
    {
        networkView.RPC("TellServer", RPCMode.Server, p);
        GameObject player = Network.Instantiate(playerPrefab, SpawnPosition(), Quaternion.identity, 0) as GameObject;
        player.GetComponent<Player>().player.playerID = totalPCount;
        player.GetComponent<Player>().player.playerType = type;
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

    public void OnConnectedToServer()
    {
       
    }

    public Vector3 SpawnPosition()
    {
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
