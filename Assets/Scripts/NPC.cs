using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;

public class NPC : MonoBehaviour
{
    // Infection Status
    public bool infected = false;

    Path path;
    Seeker seeker;
    CharacterController controller;
    Vector3 targetPos = new Vector3(20, 0, 45);
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

    void OnTriggerEnter(Collider c)
    {
        if (Network.isServer && c.gameObject.tag == "Player")
        {
            infected = true;
            targetPos = FindHero();
            if (Network.isServer)
                seeker.StartPath(transform.position, targetPos, OnPathComplete);
            Vector3 green = new Vector3(0, 1, 0);
            networkView.RPC("SetColor", RPCMode.AllBuffered, green);
        }
    }

    Vector3 FindHero()
    {
        // Hero list should be dynamic...And refresh with new players.


        List<GameObject> heroList = new List<GameObject>();
        GameObject[] playerList;
        playerList = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in playerList)
        {
            if (player.GetComponent<Player>().type == 2)
                heroList.Add(player);
        }
        Debug.Log("Count: " + heroList.Count);
        int r = Random.Range(0, heroList.Count() );
        Vector3 pos = heroList[r].transform.position;
        return pos;
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

    void Update()
    {
        if (NetworkManager.serverStarted && !startPathCalled)
        {
            StartPath();
            startPathCalled = true;
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