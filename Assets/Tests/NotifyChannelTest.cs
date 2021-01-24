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
    }
}
