using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public Transform player;

    private float chaseDistance = 5.5f;
    private float chaseHeight = 1.55f;
    private float followDampening = 0.5f;
    private float lookAtDampening = 20.0f;

    void Update()
    {
        if (player)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, player.rotation, Time.deltaTime * lookAtDampening);
            transform.position = Vector3.Lerp(transform.position, player.position - player.forward * chaseDistance + player.up * chaseHeight, Time.deltaTime * followDampening * 7.5f);
        }
    }
}
