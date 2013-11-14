using UnityEngine;
using System.Collections;

public class Move_CC : MonoBehaviour
{
    public float speed;
    public float defSpeed;
    public float rotSpeed;
    CharacterController controller;
	
	void Awake ()
	{
        if (networkView.isMine)
        {
            // Apply the delta time once, because for some reason it's jumpy on builds
            // and we're ending up with jumpy movement. Not sure why this is happening. Oh well.
            speed = speed * Time.deltaTime;
            rotSpeed = rotSpeed * Time.deltaTime;

            defSpeed = speed;
            controller = gameObject.GetComponent<CharacterController>();
        }
	}
	
	void Update ()
	{
        if (networkView.isMine)
        {
            // Player rotation (Couple an animation with this later)
            transform.Rotate(0, Input.GetAxis("Horizontal") * rotSpeed, 0);

            // When we move the player forward, we're moving in local space.
            Vector3 forward = transform.TransformDirection(Vector3.forward);

            // If the player is moving forwards, speed is normal. If moving backwards, move slower.
            if (Input.GetAxis("Vertical") > 0.5f)
                controller.SimpleMove(forward * (speed * Input.GetAxis("Vertical")));
            else if (Input.GetAxis("Vertical") < -0.5f)
                controller.SimpleMove(forward * (speed / 5 * Input.GetAxis("Vertical")));
        }
	}

    void OnGUI()
    {
        // This is used for debugging.
        if (networkView.isMine)
        {
            //GUI.Label(new Rect(200, 10, 200, 20), "Speed: " + speed.ToString());
            //GUI.Label(new Rect(200, 30, 200, 20), "Axis: " + Input.GetAxis("Vertical").ToString());
        }
    }
}
