using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class NetworkManager : MonoBehaviour 
{
    public struct PLAYER
    {
        public NetworkPlayer player;
        public int playerID;
    }

    public GameObject playerPrefab, playerCamera, timedMessagePrefab;
    public List<PLAYER> PLAYERS;
    private int currPlayerCount = 0, totalPlayerCount = 0;
    private double timer = 0;

    //void Awake() { Time.timeScale = 0.01f; }

    void StartServer()
    {
        Network.InitializeServer(32, 25000, false);
        Network.sendRate = 15;
        Debug.Log("Server initialized, sendRate: " + Network.sendRate);
        timer = Time.time;
        PLAYERS = new List<PLAYER>();

        NewTimedMessage("Connected as SERVER", 6);
    }

    void ConnectToServer()
    {
        Network.Connect("127.0.0.1", 25000, "");
    }

    void OnGUI()
    {
        if (!Network.isClient && !Network.isServer)
        {
            if (GUI.Button(new Rect(10, 10, 100, 20), "Start Server"))
                StartServer();

            if (GUI.Button(new Rect(120, 10, 150, 20), "Join Existing Game"))
                ConnectToServer();
        }

        if (Network.isServer)
            GUI.Label(new Rect(Screen.width - 130, 10, 125, 20), "Player Count: " + currPlayerCount.ToString());
    }

    void OnPlayerConnected(NetworkPlayer p)
    {
        totalPlayerCount++;
        currPlayerCount++;

        PLAYER newPlayer = new PLAYER();
        newPlayer.player = p;
        newPlayer.playerID = totalPlayerCount;
        PLAYERS.Add(newPlayer);

        networkView.RPC("Identification", p, totalPlayerCount);
        networkView.RPC("NewTimedMessage", RPCMode.All, "Player " + newPlayer.playerID + " connected", 5.0f);
    }

    [RPC]
    void Identification(int id)
    {
        GameObject player = Network.Instantiate(playerPrefab, SpawnPosition(), Quaternion.identity, 0) as GameObject;
        networkView.RPC("TellServer", RPCMode.Server, player.networkView.owner);
        player.GetComponent<Player>().playerID = id;
    }

    [RPC]
    void TellServer(NetworkPlayer id)
    {
        Debug.Log("Player is: ... " + id.ToString());
    }

    void OnPlayerDisconnected(NetworkPlayer p)
    {
        currPlayerCount--;

        PLAYER deadPlayer = PLAYERS.Find(PLAYER => PLAYER.player == p);
        Debug.Log("Player " + deadPlayer.playerID + " disconnected...");
        PLAYERS.RemoveAll(PLAYER => PLAYER.player == p);
        Network.RemoveRPCs(p);
        Network.DestroyPlayerObjects(p);

        networkView.RPC("NewTimedMessage", RPCMode.All, "Player " + deadPlayer.playerID + " disconnected...", 5.0f);
    }

    void OnConnectedToServer()
    {
       
    }

    Vector3 SpawnPosition()
    {
        return new Vector3(Random.Range(-15, 15), 0.66f, Random.Range(-15, 15));
    }

    [RPC]
    void NewTimedMessage(string m, float l)
    {
        GameObject timedMessage = GameObject.Instantiate(timedMessagePrefab) as GameObject;
        timedMessage.GetComponent<TimedMessage>().text = m;
        timedMessage.GetComponent<TimedMessage>().lifetime = l;
    }
}
