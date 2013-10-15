using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    public int type = 0;
    public GameObject timedMessagePrefab;
    public int playerID;

	void OnNetworkInstantiate(NetworkMessageInfo info)
    {
        if (networkView.isMine)
            Camera.main.GetComponent<CameraController>().player = transform;
    }

    void Start()
    {
        if (networkView.isMine)
            NewTimedMessage("Connected as CLIENT [Player " + Network.player.ToString() + "]", 5);
        playerID = int.Parse(Network.player.ToString());
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
            GUI.Label(new Rect(10, 250, 200, 20), "Type: " + type);
        }
    }
}
