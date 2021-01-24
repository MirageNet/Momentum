using System;

namespace Mirror.Momentum
{
    /*
     * A channel that 
     * - DONT provide any reliable delivery
     * - DO   protect against duplicates and out-of-order packets
     * - We DO   tell the sender *if* the packet arrived
     * - We DO   tell the sender if the packet *most likely* was lost
     */

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
    /// </remarks>
    /// 
    public class NotifyChannel : INotifyChannel
    {
        private readonly INetworkConnection connection;
        private Sequencer sequencer;

        public event Action<ArraySegment<byte>, int> PacketReceived;
        public event Action<int> PacketLost;
        public event Action<int> PacketDelivered;

        public NotifyChannel(INetworkConnection connection)
        {
            sequencer = new Sequencer(16);
            this.connection = connection;

            this.connection.RegisterHandler<NotifyPacket>(Receive);
        }

        private void Receive(NotifyPacket arg2)
        {
            throw new NotImplementedException();
        }

        public void Send(ArraySegment<byte> payload, int token)
        {
            var packet = new NotifyPacket
            {
                Sequence = (ushort)sequencer.Next(),
                ReceiveSequence = 0,
                AckMask = 0,
                Payload = payload
            };

            connection.Send(packet, Channel.Unreliable);
        }
    }




}