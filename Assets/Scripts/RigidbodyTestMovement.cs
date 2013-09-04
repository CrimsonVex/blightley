using UnityEngine;
using System.Collections;

public class RigidbodyTestMovement : MonoBehaviour
{
    private float rot = 0;
    private float speed = 0;

    void Update()
    {
        if (Input.GetKey("left"))
            rot -= 5f * Time.deltaTime;
        if (Input.GetKey("right"))
            rot += 5f * Time.deltaTime;

        if (Input.GetKey("up"))
            speed += 1f * Time.deltaTime;
        if (Input.GetKey("down"))
            speed -= 1f * Time.deltaTime;

        rigidbody.AddRelativeForce(Vector3.forward * speed);
        rigidbody.AddRelativeTorque(new Vector3(0, rot, 0));
    }
}