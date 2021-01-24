using System;

namespace Mirror.Momentum
{
    public struct NotifyPacket
    {
        public ushort Sequence;
        public ushort ReceiveSequence;
        public ulong AckMask;

        public ArraySegment<byte> Payload;
    }
}