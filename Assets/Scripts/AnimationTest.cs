using UnityEngine;
using System.Collections;

public class AnimationTest : MonoBehaviour
{
    public AnimationClip run, jump;
    private Animation _anim;

	void Start()
    {
        _anim = this.GetComponent<Animation>();
        _anim.Play("Run");
	}

    void Update()
    {
        if (this.gameObject.animation["Jump"].time >= 0.4f)
            _anim.CrossFade(run.name, 0.2f);
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(300, 20, 200, 25), "Jump"))
        {
            networkView.RPC("Jump", RPCMode.All, "Jump");
        }
    }

    [RPC]
    void Jump(string action)
    {
        _anim.CrossFade(jump.name, 0.2f);
    }
}
