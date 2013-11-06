using UnityEngine;
using System.Collections;

public class Move : MonoBehaviour
{
    public float movementModifier;
    public float rotationModifier;

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
            rigidbody.AddRelativeForce(Vector3.forward * Input.GetAxis("Vertical") * movementModifier);
            rigidbody.AddRelativeTorque(new Vector3(0, Input.GetAxis("Horizontal") * rotationModifier, 0));
        }
    }
}