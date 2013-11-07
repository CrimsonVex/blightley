using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    public GameObject timedMessagePrefab;
    public int playerID;
    public NetworkManager.PLAYER player = new NetworkManager.PLAYER();

	void OnNetworkInstantiate(NetworkMessageInfo i)
    {
        if (networkView.isMine)
            Camera.main.GetComponent<CameraController>().player = transform;

        if (Network.isServer)
        {
            int index = NetworkManager.Instance.PLAYERS.FindIndex(a => a.player == gameObject.networkView.owner);
            NetworkManager.PLAYER p = new NetworkManager.PLAYER();
            p.player = NetworkManager.Instance.PLAYERS[index].player;
            p.playerID = NetworkManager.Instance.PLAYERS[index].playerID;
            p.playerType = NetworkManager.Instance.PLAYERS[index].playerType;
            p.pObject = gameObject;
            NetworkManager.Instance.PLAYERS[index] = p;
        }
    }

    void Start()
    {
        if (networkView.isMine)
            NewTimedMessage("Connected as CLIENT [Player " + Network.player.ToString() + "]", 5);
        playerID = int.Parse(Network.player.ToString());
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
