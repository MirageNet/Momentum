using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using System;

public class Bandwidth : MonoBehaviour
{
    ExponentialMovingAverage receiveAverage = new ExponentialMovingAverage(30);
    ExponentialMovingAverage sendAverage = new ExponentialMovingAverage(30);

    public Transport transport;
    public Text text;

    [Tooltip("How many seconds to consider when calculating the average bandwidth")]
    [Range(1,30)]
    public int window = 5;

    public void Start()
    {
        receiveAverage = new ExponentialMovingAverage(window);

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

            receiveAverage.Add(receivedDiff);
            sendAverage.Add(sentDiff);

            prevReceivedBytes = receivedBytes;
            prevSentBytes = sentBytes;
            text.text = $"↓{Format(receiveAverage.Value)} ↑{Format(sendAverage.Value)}";
        }
    }

    private string Format(double bytesPerSecond)
    {
        double bitsPerSecond = bytesPerSecond * 8;

        if (bitsPerSecond >= 1_000_000)
        {
            double formatted = bitsPerSecond / 1_000_000.0f;
            return $"{formatted:G3} Mbps";
        }
        if (bitsPerSecond >= 1_000)
        {
            double formatted = bitsPerSecond / 1_000.0f;
            return $"{formatted:G3} Kbps";
        }
        return $"{bitsPerSecond:G3} bps";
    }
}
