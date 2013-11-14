using UnityEngine;
using System.Collections;

public class PlayerAnimation : MonoBehaviour
{
    public Animation _anim;
    public enum STATE { Idle, Walk, Run, Attack, Wince, Death, Crouch, BipedToRun };
    public string[] animations = new string[8];
    private string oldAnimation, currentAnimation;
    private STATE s;
    private bool isWalking = false;
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

        if (networkView.isMine)
        {
            networkView.RPC("setAnimationState", RPCMode.Others, 0);
            setAnimationState(0);
            s = STATE.Idle;
        }
    }

    void Update()
    {
        if (networkView.isMine)
        {
            if (s == STATE.Idle) setAnimation(animations[0]);
            if (s == STATE.Walk) setAnimation(animations[1]);
            if (s == STATE.Run) setAnimation(animations[2]);
            if (s == STATE.Attack) setAnimation(animations[3]);
            if (s == STATE.Wince) setAnimation(animations[4]);
            if (s == STATE.Death) setAnimation(animations[5]);
            if (s == STATE.Crouch) setAnimation(animations[6]);
            if (s == STATE.BipedToRun) setAnimation(animations[7]);

            if (Input.GetAxis("Vertical") > 0.5f)
            {
                if (s != STATE.Run || (Input.GetButtonUp("Sprint") && s == STATE.Run))
                    Walk();
                if (Input.GetButtonDown("Sprint"))
                {
                    if (s == STATE.Run)
                        Walk();
                    else
                        Run();
                }

            }
            else if (s == STATE.Walk || s == STATE.Run)
                s = STATE.Idle;

            if (Input.GetButtonDown("Jump"))
                s = STATE.Attack;

            if (Input.GetButtonDown("Crouch"))
            {
                if (s == STATE.Crouch)
                    s = STATE.Idle;
                else
                    s = STATE.Crouch;
            }
        }

        // Ensure the default animations only play if another animation isnt already playing
        // New connecting players need to see other players at their current correct frame for their animations

        // Animations are kind of irrelevant
        // You should be sending gameplay events that should trigger animations locally
        // Screw sending animations
    }

    void Walk()
    {
        s = STATE.Walk;

        // This controls the movement speed of the player for either walk or sprint.
        if (gameObject.name == "NewPlayer(Clone)")
            gameObject.GetComponent<Move>().movementModifier = gameObject.GetComponent<Move>().movementModifierDefault;
        else if (gameObject.name == "NewPlayer_CC(Clone)")
            gameObject.GetComponent<Move_CC>().speed = gameObject.GetComponent<Move_CC>().defSpeed;
    }

    void Run()
    {
        s = STATE.Run;

        // This controls the movement speed of the player for either walk or sprint.
        if (gameObject.name == "NewPlayer(Clone)")
            gameObject.GetComponent<Move>().movementModifier = gameObject.GetComponent<Move>().movementModifierDefault * 3;
        else if (gameObject.name == "NewPlayer_CC(Clone)")
            gameObject.GetComponent<Move_CC>().speed = gameObject.GetComponent<Move_CC>().defSpeed * 2.5f;
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