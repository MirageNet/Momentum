using NSubstitute;
using NUnit.Framework;
using System;
using Random = UnityEngine.Random;

namespace Mirror.Momentum
{
    public class NotifyChannelTest
    {

        private INetworkConnection connection;
        private INotifyChannel notifyChannel;
        private ArraySegment<byte> data;

        private event Action<NotifyPacket> ReceiveEvent;

        [SetUp]
        public void SetUp()
        {
            connection = Substitute.For<INetworkConnection>();

            connection
                .When(connection => connection.RegisterHandler(Arg.Any<Action<NotifyPacket>>()))
                .Do(args => ReceiveEvent += args.ArgAt<Action<NotifyPacket>>(0));

            notifyChannel = new NotifyChannel(connection);

            data = new ArraySegment<byte>(new byte[Random.Range(1, 255)]);

            connection.Received().RegisterHandler(Arg.Any<Action<NotifyPacket>>());
        }

        [Test]
        public void SendsNotifyPacket()
        {
            notifyChannel.Send(data, 1);

            connection.Received().Send(Arg.Any<NotifyPacket>(), Channel.Unreliable);
        }

        [Test]
        public void SendsNotifyPacketWithSequence()
        {
            notifyChannel.Send(data, 1);
            notifyChannel.Send(data, 1);
            notifyChannel.Send(data, 1);

            Received.InOrder(() =>
            {
                connection.Received().Send(Arg.Is<NotifyPacket>(packet => packet.Sequence == 1), Channel.Unreliable);
                connection.Received().Send(Arg.Is<NotifyPacket>(packet => packet.Sequence == 2), Channel.Unreliable);
                connection.Received().Send(Arg.Is<NotifyPacket>(packet => packet.Sequence == 3), Channel.Unreliable);
            });
        }

        [Test]
        public void SendsNotifyPacketWithReceiveSequence()
        {
            notifyChannel.Send(data, 1);
            connection.Received().Send(Arg.Is<NotifyPacket>(packet => packet.ReceiveSequence == 0), Channel.Unreliable);
        }

        [Test]
        public void SendsNotifyPacketWithAckMask()
        {
            notifyChannel.Send(data, 1);
            connection.Received().Send(Arg.Is<NotifyPacket>(packet => packet.AckMask == 0), Channel.Unreliable);
        }

        [Test]
        public void SendsNotifyPacketWithData()
        {
            notifyChannel.Send(data, 1);
            connection.Received().Send(Arg.Is<NotifyPacket>(packet => packet.Payload == data), Channel.Unreliable);
        }

        [Test]
        public void RaisePacketDelivered()
        {
            Action<int> delivered = Substitute.For<Action<int>>();

            notifyChannel.PacketDelivered += delivered;

            notifyChannel.Send(data, 1);
            notifyChannel.Send(data, 3);
            notifyChannel.Send(data, 5);

            delivered.DidNotReceive().Invoke(Arg.Any<int>());

            var reply = new NotifyPacket
            {
                Sequence = 1,
                ReceiveSequence = 3,
                AckMask = 0b111,
                Payload = data
            };

            ReceiveEvent?.Invoke(reply);

            Received.InOrder(() =>
            {
                delivered.Invoke(1);
                delivered.Invoke(3);
                delivered.Invoke(5);
            });
        }

        [Test]
        public void RaisePacketNotDelivered()
        {
            Action<int> delivered = Substitute.For<Action<int>>();
            Action<int> lost = Substitute.For<Action<int>>();

            notifyChannel.PacketDelivered += delivered;
            notifyChannel.PacketLost += lost;

            notifyChannel.Send(data, 1);
            notifyChannel.Send(data, 3);
            notifyChannel.Send(data, 5);

            delivered.DidNotReceive().Invoke(Arg.Any<int>());

            var reply = new NotifyPacket
            {
                Sequence = 1,
                ReceiveSequence = 3,
                AckMask = 0b001,
                Payload = data
            };

            ReceiveEvent?.Invoke(reply);

            Received.InOrder(() =>
            {
                lost.Invoke(1);
                lost.Invoke(3);
                delivered.Invoke(5);
            });
        }

        [Test]
        public void LoseOldPackets()
        {
            for (int i = 1; i< 10; i++)
            {
                var packet = new NotifyPacket
                {
                    Sequence = (ushort)i,
                    ReceiveSequence = 100,
                    AckMask = ~0b0ul,
                    Payload = data
                };
                ReceiveEvent?.Invoke(packet);

            }

            var reply = new NotifyPacket
            {
                Sequence = 100,
                ReceiveSequence = 100,
                AckMask = ~0b0ul,
                Payload = data
            };
            ReceiveEvent?.Invoke(reply);

            notifyChannel.Send(data, 1);

            connection.Received().Send(
                Arg.Is<NotifyPacket>(packet => packet.AckMask == 1 && packet.ReceiveSequence == 100),
                Channel.Unreliable);
        }

        [Test]
        public void NotAcknoledgedYet()
        {
            Action<int> delivered = Substitute.For<Action<int>>();
            Action<int> lost = Substitute.For<Action<int>>();

            notifyChannel.PacketDelivered += delivered;
            notifyChannel.PacketLost += lost;

            notifyChannel.Send(data, 1);
            notifyChannel.Send(data, 3);
            notifyChannel.Send(data, 5);

            var reply = new NotifyPacket
            {
                Sequence = 1,
                ReceiveSequence = 2,
                AckMask = 0b011,
                Payload = data
            };

            ReceiveEvent?.Invoke(reply);

            delivered.DidNotReceive().Invoke(5);

            reply = new NotifyPacket
            {
                Sequence = 2,
                ReceiveSequence = 3,
                AckMask = 0b111,
                Payload = data
            };
            ReceiveEvent?.Invoke(reply);

            delivered.Received().Invoke(5);
        }

    }
}
