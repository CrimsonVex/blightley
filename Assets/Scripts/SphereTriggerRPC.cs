using UnityEngine;
using System.Collections;

public class SphereTriggerRPC : MonoBehaviour
{
    public GameObject timedMessagePrefab;
    private Vector3 blue, green, red;
    private string currentPlayerInside;

    void Awake()
    {
        blue = new Vector3(0, 0.49f, 1);
        green = new Vector3(0, 1, 0);
        red = new Vector3(1, 0, 0);
    }

    void OnTriggerEnter(Collider c)
    {
        Debug.Log(c.tag);
        if (Network.isServer && c.gameObject.tag == "Player")
        {
            networkView.RPC("NewTimedMessage", RPCMode.All, "Player " + c.networkView.owner + " lit the beacon!", 5.0f);
            Vector3 green = new Vector3(0, 1, 0);
            networkView.RPC("SetColor", RPCMode.AllBuffered, green);
            currentPlayerInside = c.networkView.owner.ToString();
        }
    }

    void OnTriggerExit(Collider c)
    {
        if (Network.isServer && c.gameObject.tag == "Player")
        {
            networkView.RPC("SetColor", RPCMode.AllBuffered, blue);
            currentPlayerInside = (-1).ToString();
        }
    }

    void OnPlayerDisconnected(NetworkPlayer p)
    {
        if (currentPlayerInside.ToString() == p.ToString())
            networkView.RPC("SetColor", RPCMode.AllBuffered, blue);
    }

    [RPC]
    void SetColor(Vector3 c)
    {
        renderer.material.color = new Color(c.x, c.y, c.z, 1);
    }

    [RPC]
    void NewTimedMessage(string m, float l)
    {
        GameObject timedMessage = GameObject.Instantiate(timedMessagePrefab) as GameObject;
        timedMessage.GetComponent<TimedMessage>().text = m;
        timedMessage.GetComponent<TimedMessage>().lifetime = l;
    }
}
