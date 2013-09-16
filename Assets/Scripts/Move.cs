using UnityEngine;
using System.Collections;

public class Move : MonoBehaviour
{
    
    void Awake()
    {
        if (networkView.isMine)
        {

        }
    }

    void Update()
    {
        if (networkView.isMine)
        {
            rigidbody.AddRelativeForce(Vector3.forward * 10 * Input.GetAxis("Vertical"));
            rigidbody.AddRelativeTorque(new Vector3(0, Input.GetAxis("Horizontal"), 0));
        }
    }
}