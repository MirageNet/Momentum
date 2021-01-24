using System;

namespace Mirror.Momentum
{
    public interface INotifyChannel
    {
        event Action<ArraySegment<byte>, int> PacketReceived;
        event Action<int> PacketLost;
        event Action<int> PacketDelivered;

        void Send(ArraySegment<byte> payload, int token);
    }
}