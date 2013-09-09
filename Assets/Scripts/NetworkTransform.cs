using UnityEngine;
using System.Collections;

public class NetworkTransform : MonoBehaviour
{
    private double m_InterpolationBackTime = 0.1f;
    private double m_ExtrapolationLimit = 0.5f;

    internal struct State
    {
        internal double timestamp;
        internal Vector3 pos;
        internal Quaternion rot;
    }

    // We store twenty states with "playback" information
    State[] m_BufferedState = new State[20];
    // Keep track of what slots are used
    int m_TimestampCount;

    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        // Send data to server
        if (stream.isWriting)
        {
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            stream.Serialize(ref pos);
            stream.Serialize(ref rot);
        }
        // Read data from remote client
        else
        {
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            stream.Serialize(ref pos);
            stream.Serialize(ref rot);

            // Shift the buffer sideways, deleting last state
            for (int i = m_BufferedState.Length - 1; i >= 1; i--)
                m_BufferedState[i] = m_BufferedState[i - 1];

            // Record current state in slot 0
            State state;
            state.timestamp = info.timestamp;
            state.pos = pos;
            state.rot = rot;
            m_BufferedState[0] = state;

            // Update used slot count, however never exceed the buffer size
            // Slots aren't actually freed so this just makes sure the buffer is
            // filled up and that uninitalized slots aren't used.
            m_TimestampCount = Mathf.Min(m_TimestampCount + 1, m_BufferedState.Length);

            // Check if states are in order, if it is inconsistent you could reshuffel or 
            // drop the out-of-order state. Nothing is done here
            for (int i = 0; i < m_TimestampCount - 1; i++)
            {
                if (m_BufferedState[i].timestamp < m_BufferedState[i + 1].timestamp)
                    Debug.Log("State inconsistent");
            }
        }
    }

    // We have a window of interpolationBackTime where we basically play 
    // By having interpolationBackTime the average ping, you will usually use interpolation.
    // And only if no more data arrives we will use extra polation
    void Update()
    {
        // This is the target playback time of the rigid body
        double interpolationTime = Network.time - m_InterpolationBackTime;

        // Use interpolation if the target playback time is present in the buffer
        if (m_BufferedState[0].timestamp > interpolationTime)
        {
            // Go through buffer and find correct state to play back
            for (int i = 0; i < m_TimestampCount; i++)
            {
                if (m_BufferedState[i].timestamp <= interpolationTime || i == m_TimestampCount - 1)
                {
                    // The state one slot newer (<100ms) than the best playback state
                    State rhs = m_BufferedState[Mathf.Max(i - 1, 0)];
                    // The best playback state (closest to 100 ms old (default time))
                    State lhs = m_BufferedState[i];

                    // Use the time between the two slots to determine if interpolation is necessary
                    double length = rhs.timestamp - lhs.timestamp;
                    float t = 0;
                    // As the time difference gets closer to 100 ms t gets closer to 1 in 
                    // which case rhs is only used
                    // Example:
                    // Time is 10.000, so sampleTime is 9.900 
                    // lhs.time is 9.910 rhs.time is 9.980 length is 0.070
                    // t is 9.900 - 9.910 / 0.070 = 0.14. So it uses 14% of rhs, 86% of lhs
                    if (length > 0.0001f)
                        t = (float)((interpolationTime - lhs.timestamp) / length);

                    // if t=0 => lhs is used directly
                    transform.localPosition = Vector3.Lerp(lhs.pos, rhs.pos, t);
                    transform.localRotation = Quaternion.Slerp(lhs.rot, rhs.rot, t);
                    return;
                }
            }
        }
        // Use extrapolation
        else
        {
            State latest = m_BufferedState[0];

            float extrapolationLength = (float)(interpolationTime - latest.timestamp);
            // Don't extrapolation for more than 500 ms, you would need to do that carefully
            if (extrapolationLength < m_ExtrapolationLimit)
            {
                transform.position = latest.pos;
                transform.rotation = latest.rot;
            }
        }
    }
}
