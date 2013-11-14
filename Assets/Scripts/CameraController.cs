using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    // A camera object that will follow and look at the player with some parametres for positions and angles

    public Transform player;

    private float chaseDistance = 5.5f;
    private float chaseHeight = 5.0f;
    private float followDampening = 0.5f;
    private float lookAtDampening = 20.0f;

    void Update()
    {
        if (player)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(30, player.rotation.eulerAngles.y, player.rotation.eulerAngles.z)), Time.deltaTime * lookAtDampening);
            transform.position = Vector3.Lerp(transform.position, player.position - player.forward * chaseDistance + player.up * chaseHeight, Time.deltaTime * followDampening * 7.5f);
        }
    }
}
