using UnityEngine;
using System.Collections;

public class AnimationTest : MonoBehaviour
{
    public AnimationClip walk, run, attack, wince, death, bipedToRun, idle, crouch;
    private Animation _anim;
    bool playState = false;
    char animID;

	void Start()
    {
        _anim = this.GetComponent<Animation>();
        _anim.Play("Idle");
        playState = true;
        animID = 'I';
	}

    void Update()
    {
        if (networkView.isMine)
        {
            if (animID == 'A' && this.gameObject.animation["Attack"].time >= 0.708f)
            {
                _anim.CrossFade(idle.name, 0.2f);
                animID = 'I';
            }

            if (Input.GetButtonDown("Jump"))
                animID = 'A';

            if (Input.GetAxis("Vertical") > 0.5f)
                animID = 'W';
            else
                animID = 'I';

            switch (animID)
            {
                case 'I':
                    _anim.Play("Idle");
                    break;
                case 'W':
                    _anim.Play("Walk");
                    break;
                case 'R':
                    _anim.Play("Run");
                    break;
                case 'A':
                    _anim.Play("Attack");
                    break;
                case 'H':
                    _anim.Play("Wince");
                    break;
                case 'D':
                    _anim.Play("Death");
                    break;
                case 'B':
                    _anim.Play("BipedToRun");
                    break;
                case 'C':
                    _anim.Play("Crouch");
                    break;
            }
        }
    }

    //void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    //{
    //    //this is what we serialize TO the network
    //    if (stream.isWriting)
    //    {
    //        bool sendPlayState = playState;
    //        char sendAnimID = animID;

    //        stream.Serialize(ref sendPlayState);
    //        stream.Serialize(ref sendAnimID);
    //    }
    //    //this is what we serialize FROM the network
    //    else
    //    {
    //        bool receivePlayState = false;
    //        char receiveAnimID = '0';

    //        stream.Serialize(ref receivePlayState);
    //        stream.Serialize(ref receiveAnimID);

    //        playState = receivePlayState;
    //        animID = receiveAnimID;
    //    }
    //}
}
