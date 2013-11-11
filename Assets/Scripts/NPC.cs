using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;

public class NPC : MonoBehaviour
{
    // Infection Status
    public bool infected = false;
    public int id = -1;
    bool findHero = false;
    bool isFindingHero = false;
    float pathUpdateTimeSince = 0f;

    Path path;
    Seeker seeker;
    CharacterController controller;

    NetworkManager.PLAYER targetPlayer;
    Vector3 targetPos = new Vector3(40, 0, 40);

    float speed = 100;
    float nextWaypointDistance = 3;
    int currentWaypoint = 0;
    
    bool startPathCalled = false;

    public void StartPath()
    {
        if (Network.isServer)
        {
            seeker = GetComponent<Seeker>();
            controller = GetComponent<CharacterController>();
            seeker.StartPath(transform.position, targetPos, OnPathComplete);
        }
    }

    void OnGUI()
    {
        if (Network.isServer)
        {

        }
    }

    NetworkManager.PLAYER FindInstigator(Collider c)
    {
        NetworkManager.PLAYER p = NetworkManager.Instance.PLAYERS.Find(i => i.player == c.networkView.owner);
        return p;
    }

    Vector3 FindTarget(Vector3 oldPos)
    {
        targetPlayer = NetworkManager.Instance.PLAYERS.Find(i => i.playerType == 2);
        Debug.Log("Found target object: Player " + targetPlayer.playerID);
        if (targetPlayer.pObject == null)
        {
            Debug.Log("NPC can't find a target. Is there a TYPE 2 player in-game?");
            return oldPos;
        }
        else
        {
            NetworkManager.NPC npc = NetworkManager.Instance.NPCS.Find(i => i.npcID == id);
            npc.target = targetPlayer.pObject.transform.position;
            return npc.target;
        }
    }

    void OnTriggerEnter(Collider c)
    {
        if (!infected && Network.isServer && c.gameObject.tag == "Player")
        {
            NetworkManager.PLAYER p = FindInstigator(c);
            Debug.Log("Player: " + p.playerID + " infected the NPC");

            infected = true;

            int index = NetworkManager.Instance.NPCS.FindIndex(n => n.npcID == id);
            NetworkManager.NPC thisNPC = new NetworkManager.NPC();
            thisNPC.npc = NetworkManager.Instance.NPCS[index].npc;
            thisNPC.npcID = NetworkManager.Instance.NPCS[index].npcID;
            thisNPC.npcType = 1;
            thisNPC.npcObject = NetworkManager.Instance.NPCS[index].npcObject;
            thisNPC.target = NetworkManager.Instance.NPCS[index].target;
            NetworkManager.Instance.NPCS[index] = thisNPC;

            NetworkManager.Instance.npcTotalInfected++;
            targetPos = FindTarget(targetPos);

            if (!isFindingHero)
                findHero = true;
            
            Vector3 green = new Vector3(0, 1, 0);
            networkView.RPC("SetColor", RPCMode.AllBuffered, green);
        }
    }

    public void OnPathComplete(Path p)
    {
        if (Network.isServer)
        {
            Debug.Log("Yay, we got a path back. Did it have an error? " + p.error);

            if (!p.error)
            {
                path = p;
                currentWaypoint = 0;
            }
        }
    }

    // If infected
        // If hero, PURSUE
            // If hero diconnects, find new hero and PURSUE
                // If no new hero, PATROL
        // If not, PATROL
    // else ROAM

    void Start()
    {
        if (Network.isServer)
        {
            NetworkManager.NPC npc = new NetworkManager.NPC();
            npc.npc = gameObject;
            npc.npcID = id;
            npc.npcType = 0;
            npc.npcObject = gameObject;
            npc.target = targetPos;
            NetworkManager.Instance.NPCS.Add(npc);
        }
    }

    void Update()
    {
        if (NetworkManager.Instance.serverStarted && !startPathCalled)
        {
            StartPath();
            startPathCalled = true;
        }
        else if (Network.isServer && NetworkManager.Instance.serverStarted && startPathCalled && findHero)
        {
            if ((pathUpdateTimeSince += Time.deltaTime) > 1.1f)
            {
                pathUpdateTimeSince = 0f;
                targetPos = FindTarget(targetPos);
                seeker.StartPath(transform.position, targetPos, OnPathComplete);
                Debug.Log("Infected NPC now tracking enemy hero (Player " + targetPlayer.playerID + " at position: " + targetPos);
            }
        }
    }

    void FixedUpdate()
    {
        if (Network.isServer)
        {
            if (path == null)
            {
                Debug.Log("No path");
                return;
            }

            if (currentWaypoint >= path.vectorPath.Count)
            {
                targetPos = new Vector3(Random.Range(-40, 40), 1, Random.Range(-40, 40));
                Debug.Log("End Of Path Reached");
                return;
            }

            // Direction to the next waypoint
            Vector3 dir = (path.vectorPath[currentWaypoint] - transform.position).normalized;
            dir *= speed * Time.fixedDeltaTime;
            controller.SimpleMove(dir);

            // Check if we are close enough to the next waypoint, if we are, proceed to follow the next waypoint
            if (Vector3.Distance(transform.position, path.vectorPath[currentWaypoint]) < nextWaypointDistance / 2)
            {
                currentWaypoint++;
                return;
            }
        }
    }

    [RPC]
    void SetColor(Vector3 c)
    {
        renderer.material.color = new Color(c.x, c.y, c.z, 1);
    }
}