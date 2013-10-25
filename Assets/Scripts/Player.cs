using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    public int type = 0;
    public GameObject timedMessagePrefab;
    public int playerID;
    public NetworkManager.PLAYER player = new NetworkManager.PLAYER();

	void OnNetworkInstantiate(NetworkMessageInfo i)
    {
        if (networkView.isMine)
            Camera.main.GetComponent<CameraController>().player = transform;

        if (Network.isServer)
        {
            Debug.Log("A");
            int index = NetworkManager.Instance.PLAYERS.FindIndex(a => a.player == gameObject.networkView.owner);
            NetworkManager.PLAYER p = new NetworkManager.PLAYER();
            p.player = NetworkManager.Instance.PLAYERS[index].player;
            p.playerID = NetworkManager.Instance.PLAYERS[index].playerID;
            p.playerType = NetworkManager.Instance.PLAYERS[index].playerType;
            p.pObject = gameObject;
            NetworkManager.Instance.PLAYERS[index] = p;
            Debug.Log("B: " + NetworkManager.Instance.PLAYERS[index].pObject.transform.position);
        }
    }

    void Start()
    {
        if (networkView.isMine)
            NewTimedMessage("Connected as CLIENT [Player " + Network.player.ToString() + "]", 5);
        playerID = int.Parse(Network.player.ToString());
    }

    void Update()
    {

    }

    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        if (stream.isWriting)   // Send data to server
        {
            int t = type;
            stream.Serialize(ref t);
        }
        else
        {                // Read data from remote client
            int t = 0;
            stream.Serialize(ref t);
            type = t;
        }
    }

    void NewTimedMessage(string m, float l)
    {
        GameObject timedMessage = GameObject.Instantiate(timedMessagePrefab) as GameObject;
        timedMessage.GetComponent<TimedMessage>().text = m;
        timedMessage.GetComponent<TimedMessage>().lifetime = l;
    }

    void OnGUI()
    {
        if (networkView.isMine)
        {
            GUI.Label(new Rect(10, 200, 200, 20), playerID.ToString() + "     " + networkView.owner);
            GUI.Label(new Rect(10, 250, 200, 20), "Type: " + player.playerType);
            GUI.Label(new Rect(10, 150, 200, 20), "I am Player: " + player.playerID);
        }
    }
}
