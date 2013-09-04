using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
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

    void NewTimedMessage(string m, float l)
    {
        GameObject timedMessage = GameObject.Instantiate(timedMessagePrefab) as GameObject;
        timedMessage.GetComponent<TimedMessage>().text = m;
        timedMessage.GetComponent<TimedMessage>().lifetime = l;
    }

    void OnGUI()
    {
        if (networkView.isMine)
            GUI.Label(new Rect(10, 200, 200, 20), playerID.ToString() + "     " + networkView.owner);
    }
}
