using UnityEngine;
using System.Collections;
using System.Linq;

public class Player : MonoBehaviour
{
    public GameObject timedMessagePrefab;
    public int playerID;    // The player's ID. The client makes use of this.
    public NetworkManager.PLAYER player = new NetworkManager.PLAYER();  // A local copy of what the server stores for the client

	void OnNetworkInstantiate(NetworkMessageInfo i)
    {
        if (networkView.isMine)
            Camera.main.GetComponent<CameraController>().player = transform;    // Tell the camera to track this player object

        if (Network.isServer)
        {
            // To update values in the server's PLAYER list, we need to make a new PLAYER object entirely
            int index = NetworkManager.Instance.PLAYERS.FindIndex(a => a.player == gameObject.networkView.owner);
            NetworkManager.PLAYER p = new NetworkManager.PLAYER();
            p.player = NetworkManager.Instance.PLAYERS[index].player;
            p.playerID = NetworkManager.Instance.PLAYERS[index].playerID;
            p.playerType = NetworkManager.Instance.PLAYERS[index].playerType;
            p.pObject = gameObject;     // This is all we're changing - we're giving the server a reference to this object
            NetworkManager.Instance.PLAYERS[index] = p;
        }
    }

    static Transform FindChild(Transform parent, string name)   // Find a child object based on a GameObject name
    {
        return parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == name);
    }

    void Start()
    {
        if (networkView.isMine)
        {
            NewTimedMessage("Connected as CLIENT [Player " + Network.player.ToString() + "]", 5);
        
            Vector3 red = new Vector3(1, 0, 0);
            Vector3 blue = new Vector3(0, 0, 1);

            if (player.playerType == 1)     // Setup some temporary colours for obvious player types
                networkView.RPC("TellAll", RPCMode.AllBuffered, "werewolf_mesh01", red);
            else if (player.playerType == 2)
                networkView.RPC("TellAll", RPCMode.AllBuffered, "werewolf_mesh01", blue);
        }

        playerID = int.Parse(Network.player.ToString());
    }

    [RPC]
    public void TellAll(string childName, Vector3 col)      // This RPC is called for all current and future clients
    {
        // Used to set the color of all players across the server.
        FindChild(transform, childName).gameObject.renderer.material.SetColor("_Color", new Color(col.x, col.y, col.z));
    }

    void OnGUI()
    {
        if (networkView.isMine)     // Just some debugging feedback for the client
        {
            GUI.Label(new Rect(10, 150, 200, 20), "I am Player: " + player.playerID);
            GUI.Label(new Rect(10, 180, 200, 20), "Type: " + player.playerType);
        }
    }

    void NewTimedMessage(string m, float l)     // Man I should really make this a static function...
    {
        GameObject timedMessage = GameObject.Instantiate(timedMessagePrefab) as GameObject;
        timedMessage.GetComponent<TimedMessage>().text = m;
        timedMessage.GetComponent<TimedMessage>().lifetime = l;
    }
}
