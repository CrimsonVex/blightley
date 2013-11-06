using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;

public class NPCSpawner : MonoBehaviour
{
    public int npcCount = 3;
    public GameObject npcPrefab;

    private int currentNPCID = 0;
    private List<NetworkManager.NPC> infectedList = new List<NetworkManager.NPC>();

    void OnServerInitialized()
	{
        if (Network.isServer)
        {
            if (npcPrefab)
            {
                for (int i = 0; i < npcCount; i++)
                {
                    Vector3 randomPos = new Vector3(Random.Range(-40, 40), npcPrefab.transform.position.y, Random.Range(-40, 40));
                    Node node = (Node)AstarPath.active.GetNearest(randomPos);
                    Vector3 openPoint = (Vector3)node.position;
                    openPoint = new Vector3(openPoint.x, npcPrefab.transform.position.y, openPoint.z);
                    GameObject npc = Network.Instantiate(npcPrefab, openPoint, Quaternion.identity, 0) as GameObject;
                    npc.GetComponent<NPC>().id = currentNPCID++;
                }
            }
            else
                Debug.LogError("npcPrefab appears to be NULL");
        }
	}

    void OnGUI()
    {
        if (Network.isServer)
        {
            GUI.Label(new Rect(400, 250, 300, 20), "Number of total infected NPCs: " + NetworkManager.Instance.npcTotalInfected.ToString());
            GUI.Label(new Rect(400, 280, 300, 20), "Number of current infected NPCs: " + GetCurrentInfectedCount().ToString());
        }
    }

    int GetCurrentInfectedCount()
    {
        infectedList = NetworkManager.Instance.NPCS.FindAll(n => n.npcType == 1);
        return infectedList.Count;
    }
}
