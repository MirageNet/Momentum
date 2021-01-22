using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;


public class Bandwidth : MonoBehaviour
{
    
    public Transport transport;
    public Text text;

    public IEnumerator Refresh()
    {
        long prevReceivedBytes = transport.ReceivedBytes;
        long prevSentBytes = transport.SentBytes;

        while (true)
        {
            yield return new WaitForSeconds(1.0f);

            long receivedBytes = transport.ReceivedBytes;
            long sentBytes = transport.SentBytes;

            long receivedDiff = receivedBytes - prevReceivedBytes;
            long sentDiff = sentBytes - prevSentBytes;

        }
    }
}
