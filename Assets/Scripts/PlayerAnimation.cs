using UnityEngine;
using System.Collections;

public class PlayerAnimation : MonoBehaviour
{
    public Animation _anim;
    public string[] animations = new string[8];
    private string oldAnimation, currentAnimation;
    //walk, run, attack, wince, death, bipedToRun, idle, crouch;

    void Start()
    {
        _anim = this.GetComponent<Animation>();
        animations[0] = "Idle";
        animations[1] = "Walk";
        animations[2] = "Run";
        animations[3] = "Attack";
        animations[4] = "Wince";
        animations[5] = "Death";
        animations[6] = "Crouch";
        animations[7] = "BipedToRun";
        networkView.RPC("setAnimationState", RPCMode.Others, 0);
        setAnimationState(0);
    }

    void Update()
    {
        if (Input.GetAxis("Vertical") > 0.5f)
            setAnimation(animations[1]);
        else
            setAnimation(animations[0]);

        if (Input.GetButtonDown("Jump"))
            setAnimation(animations[3]);
        if (Input.GetKeyDown(KeyCode.U))
            setAnimation(animations[4]);
        if (Input.GetKeyDown(KeyCode.I))
            setAnimation(animations[5]);
        if (Input.GetKeyDown(KeyCode.O) && !_anim.IsPlaying(animations[6]))
        {
            setAnimation(animations[6]);
            Debug.Log("1");
        }
        else if (Input.GetKeyDown(KeyCode.O) && _anim.IsPlaying(animations[6]))
        {
            setAnimation(animations[0]);
            Debug.Log("2");
        }

        //Debug.Log(currentAnimation + "     " + oldAnimation);

        // Ensure the default animations only play if another animation isnt already playing
        // New connecting players need to see other players at their current correct frame for their animations

        // Animations are kind of irrelevant
        // You should be sending gameplay events that should trigger animations locally
        // Fuck sending animations
    }

    void setAnimation(string state)
    {
        if (networkView.isMine)
        {
            if (currentAnimation != state)
            {
                for (int i = 0; i < animations.Length; i++)
                {
                    if (animations[i] == state)
                    {
                        if (!_anim.IsPlaying(animations[3]) && !_anim.IsPlaying(animations[4])
                            && !_anim.IsPlaying(animations[5]))
                        {
                            networkView.RPC("setAnimationState", RPCMode.Others, i);
                            setAnimationState(i);
                            oldAnimation = currentAnimation;
                            currentAnimation = state;
                            return;
                        }
                    }
                }
            }
        }
    }

    [RPC]
    void setAnimationState(int stateIndex)
    {
        _anim.animation.CrossFade(animations[stateIndex], 0.3f);
    }
}