using UnityEngine;
using System.Collections;

public class TimedMessage : MonoBehaviour
{
    public string text = "";
    public float lifetime = 5;
    public GUIStyle style;
    private float startTime, alpha = 1;
    private Rect pos;

    void Awake()
    {
        startTime = Time.time;
        GameObject[] otherTimedMessages = GameObject.FindGameObjectsWithTag("TimedMessage");
        pos = new Rect(10, 10 + (25 * (otherTimedMessages.Length - 1)), 200, 20);
    }

    void Start()
    {
        Destroy(this.gameObject, lifetime);
    }

    void Update()
    {
        if (Time.time - startTime >= lifetime - 1)
            alpha = lifetime - (Time.time - startTime);
        style.normal.textColor = new Color(1, 1, 1, alpha);
    }
    
    void OnGUI()
    {
        GUI.Label(pos, text, style);
        //GUI.Label(new Rect(10, 60, 200, 20), lifetime.ToString() + "      " + (Time.time - startTime).ToString());
    }
}
