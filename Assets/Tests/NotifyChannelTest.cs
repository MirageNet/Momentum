using System.Collections;
using NSubstitute;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mirror.Momentum
{
    public class NotifyChannelTest
    {

        private INetworkConnection connection;
        private INotifyChannel notifyChannel;

        [SetUp]
        public void SetUp()
        {
            connection = Substitute.For<INetworkConnection>();
            notifyChannel = new NotifyChannel(connection);
        }

        // A Test behaves as an ordinary method
        [Test]
        public void NotifyChannelTestSimplePasses()
        {
            // Use the Assert class to test conditions
        }

    }
}
