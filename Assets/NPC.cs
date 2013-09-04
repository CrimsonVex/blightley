using UnityEngine;
using System.Collections;

public class NPC : MonoBehaviour
{
    // Infection Status
    public bool infected = false;

    void OnTriggerEnter(Collider c)
    {
        if (c.gameObject.tag == "Player")
        {
            infected = true;
            Vector3 green = new Vector3(0, 1, 0);
            networkView.RPC("SetColor", RPCMode.AllBuffered, green);
        }
    }

    [RPC]
    void SetColor(Vector3 c)
    {
        renderer.material.color = new Color(c.x, c.y, c.z, 1);
    }
}