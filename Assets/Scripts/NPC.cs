using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;

public class NPC : MonoBehaviour
{
    // Infection Status
    public bool infected = false;       // Is this NPC infected?
    public int id = -1;                 // The current NPC's ID (get's changed from -1 by the NPCSpawner)
    bool findHero = false;              // Is it the NPC's goal to locate a hero to attack?
    float pathUpdateTimeSince = 0f;     // Used for the update interval for targetting

    // A* Pathfinding Stuff ////////////
    Path path;
    Seeker seeker;
    CharacterController controller;
    ////////////////////////////////////

    NetworkManager.PLAYER targetPlayer;     // The PLAYER object of the player target for the NPC
    Vector3 targetPos;                      // Target position for the NPC

    float speed = 100;                  // Speed of the NPC
    float nextWaypointDistance = 3;     // How close a waypoint does an NPC have to be before they switch to the next (cutting corners)
    int currentWaypoint = 0;
    
    bool startPathCalled = false;

    public void StartPath()
    {
        if (Network.isServer)
        {
            targetPos = RandomLocationFinder();
            seeker = GetComponent<Seeker>();        // A* stuff
            controller = GetComponent<CharacterController>();
            seeker.StartPath(transform.position, targetPos, OnPathComplete);
        }
    }

    NetworkManager.PLAYER FindInstigator(Collider c)    // Which player infected this NPC?
    {
        NetworkManager.PLAYER p = NetworkManager.Instance.PLAYERS.Find(i => i.player == c.networkView.owner);
        return p;
    }

    Vector3 FindTarget(Vector3 oldPos)      // Find a player of type hero to target
    {
        targetPlayer = NetworkManager.Instance.PLAYERS.Find(i => i.playerType == 2);
        Debug.Log("Found target object: Player " + targetPlayer.playerID);

        if (targetPlayer.pObject == null)
        {
            Debug.Log("NPC can't find a target. Is there a TYPE 2 (hero) player in-game?");
            return oldPos;
        }
        else
        {
            NetworkManager.NPC npc = NetworkManager.Instance.NPCS.Find(i => i.npcID == id);
            npc.target = targetPlayer.pObject.transform.position;
            return npc.target;
        }
    }

    void OnTriggerEnter(Collider c)     // Called when something triggers the NPC collider object
    {
        // This is only server side (server controls NPC's)
        // If the NPC isn't already infected, and they're collider was hit by a player...
        if (!infected && Network.isServer && c.gameObject.tag == "Player")
        {   
            NetworkManager.PLAYER p = FindInstigator(c);    // WHO DUN IT

            if (p.playerType == 1)      // If it's a mutant that 'infected' the NPC...
            {
                Debug.Log("Player: " + p.playerID + " infected the NPC");   // For debugging purposes

                // Unfortauntely to change an attribute of the NPC in the NPC list we need to make a new NPC list object entirely
                int index = NetworkManager.Instance.NPCS.FindIndex(n => n.npcID == id);
                NetworkManager.NPC thisNPC = new NetworkManager.NPC();
                thisNPC.npcObject = NetworkManager.Instance.NPCS[index].npcObject;
                thisNPC.npcID = NetworkManager.Instance.NPCS[index].npcID;
                thisNPC.npcType = 1;    // Servers variable for 'infected' (this is the only attribute we needed to change, damnit)
                thisNPC.target = NetworkManager.Instance.NPCS[index].target;
                NetworkManager.Instance.NPCS[index] = thisNPC;
                // Phew!

                NetworkManager.Instance.npcTotalInfected++;
                targetPos = FindTarget(targetPos);      // Let's find a target now...

                if (!infected)
                    infected = true;    // Local variable for infected

                Vector3 green = new Vector3(0, 1, 0);       // Just so we know they're infected...
                networkView.RPC("SetColor", RPCMode.AllBuffered, green);
            }
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
            npc.npcObject = this.gameObject;
            npc.npcID = id;
            npc.npcType = 0;
            npc.target = targetPos;
            NetworkManager.Instance.NPCS.Add(npc);

            StartPath();
        }
    }

    void Update()
    {
        if (Network.isServer && infected)
        {
            if ((pathUpdateTimeSince += Time.deltaTime) > 1.1f)     // Every 1.1 seconds the infected NPC will retarget the enemies new location
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
            if (path == null)       // We can't find a path (upon server start you'll get this, but ignore it)
            {
                Debug.Log("No path");
                return;
            }

            if (currentWaypoint >= path.vectorPath.Count)   // If the NPC reaches its current destination, find a new destination
            {                                               // unless of course we're infected, then that won't work...
                targetPos = RandomLocationFinder();
                currentWaypoint = 0;
                seeker.StartPath(transform.position, targetPos, OnPathComplete);
                Debug.Log("End Of Path Reached, new target pos is: " + Vector3.Distance(targetPos, transform.position) + " away");
                return;
            }

            // Direction to the next waypoint
            Vector3 dir = (path.vectorPath[currentWaypoint] - transform.position).normalized;
            dir *= speed * Time.fixedDeltaTime;

            // Move the NPC object.
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

    Vector3 RandomLocationFinder()  // Find somewhere that is an 'open' node that isn't inside or near a collider
    {
        // Find the operating area of the A* grid
        GridGraph gg = AstarPath.active.graphs[0] as GridGraph;
        float h = gg.depth / 2;
        float w = gg.width / 2;

        // Use the A* grid size to generate a random world location
        Node node = (Node)AstarPath.active.GetNearest(new Vector3(Random.Range(-h, w), 0, Random.Range(-h, w)));
        
        // Ensure the node we've picked is walkable by generating new positions of necessary
        while (!node.walkable)
            node = (Node)AstarPath.active.GetNearest(new Vector3(Random.Range(-h, w), 0, Random.Range(-h, w)));

        Vector3 openPoint = (Vector3)node.position;
        return openPoint;
    }
}