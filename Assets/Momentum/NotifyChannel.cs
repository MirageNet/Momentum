using System;
using System.Collections.Generic;

namespace Mirror.Momentum
{
    // EXAMPLE:
    // 1 2 3 4 5
    // 1     4 5
    //
    // DELIVERED 1 4 5 
    // LOST      2 3

    /// <summary>
    /// A data channel suitable for synchronizing snapshots.
    /// This channel works over unreliable transports
    /// and can detect when packets are lost or arrive out of order
    /// </summary>
    /// <remarks>
    ///<list type="bullet">
    ///     <item>
    ///         <description>
    ///         It does not provide any reliable delivery
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         It does protect against duplicates and out-of-order packets
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         It tells the sender if the packet arrived
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         It tells the sender if the packet most likely was lost
    ///         </description>
    ///     </item>
    /// </list>
    ///
    /// Example:
    /// 
    /// sent:     1 2 3 4 5
    /// received: 1     5 4
    /// 
    /// delivered 1 5 
    /// lost      2 3 4
    ///
    /// Losely based on: https://github.com/fholm/StreamTransport/blob/main/Transport/Transport/Peer.cs
    /// </remarks>
    /// 
    public class NotifyChannel : INotifyChannel
    {
        private readonly INetworkConnection connection;
        private Sequencer sequencer;

        public event Action<ArraySegment<byte>> PacketReceived;
        public event Action<int> PacketLost;
        public event Action<int> PacketDelivered;

        readonly Queue<PacketEnvelope> sendWindow = new Queue<PacketEnvelope>();

        private ushort receiveSequence;
        private ulong receiveMask;

        const int ACK_MASK_BITS = sizeof(ulong) * 8;

        public NotifyChannel(INetworkConnection connection)
        {
            sequencer = new Sequencer(16);
            this.connection = connection;

            this.connection.RegisterHandler<NotifyPacket>(Receive);
        }

        public void Send(ArraySegment<byte> payload, int token)
        {
            var packet = new NotifyPacket
            {
                Sequence = (ushort)sequencer.Next(),
                ReceiveSequence = receiveSequence,
                AckMask = receiveMask,
                Payload = payload
            };

            sendWindow.Enqueue(new PacketEnvelope
            {
                Sequence = packet.Sequence,
                Token = token
            });

            connection.Send(packet, Channel.Unreliable);
        }

        private void Receive(NotifyPacket packet)
        {
            int sequenceDistance = (int)sequencer.Distance(packet.Sequence, receiveSequence);

            // TODO check window size

            // this message is old,  we already received
            // a newer or duplicate packet.  Discard it
            if (sequenceDistance <= 0)
                return;

            receiveSequence = packet.Sequence;

            if (sequenceDistance >= ACK_MASK_BITS)
                receiveMask = 1;
            else
                receiveMask = (receiveMask << sequenceDistance) | 1;

            AckPackets(packet.ReceiveSequence, packet.AckMask);

            PacketReceived?.Invoke(packet.Payload);
        }

        // the other end just sent us a message
        // and it told us the latest message it got
        // and the ack mask
        private void AckPackets(ushort receiveSequence, ulong ackMask)
        {
            while (sendWindow.Count > 0)
            {
                PacketEnvelope envelope = sendWindow.Peek();

                int distance = (int)sequencer.Distance(envelope.Sequence, receiveSequence);

                if (distance > 0)
                    break;

                sendWindow.Dequeue();

                // TODO: calculate Rtt

                // if any of these cases trigger, packet is most likely lost
                if ((distance <= -ACK_MASK_BITS) || ((ackMask & (1UL << -distance)) == 0UL))
                {
                    PacketLost?.Invoke(envelope.Token);
                }
                else
                {
                    PacketDelivered?.Invoke(envelope.Token);
                }
            }
        }
    }
}