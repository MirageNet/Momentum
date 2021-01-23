using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using System;

public class Bandwidth : MonoBehaviour
{
    
    public Transport transport;
    public Text text;

    public void Start()
    {
        StartCoroutine(Refresh());
    }

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

            prevReceivedBytes = receivedBytes;
            prevSentBytes = sentBytes;
            text.text = $"↓{Format(receivedDiff)} ↑{Format(sentDiff)}";
        }
    }

    private string Format(long bytes)
    {
        if (bytes >= 1_000_000)
        {
            float formatted = bytes / 1_000_000.0f;
            return $"{formatted:G3} MBps";
        }
        if (bytes >= 1_000)
        {
            float formatted = bytes / 1_000.0f;
            return $"{formatted:G3} KBps";
        }
        return $"{bytes} Bps";
    }
}
