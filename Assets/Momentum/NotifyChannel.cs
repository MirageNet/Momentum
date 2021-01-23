using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror.Momentum
{

    public class NotifyChannel
    {
        private readonly INetworkConnection connection;

        public NotifyChannel(INetworkConnection connection)
        {
            this.connection = connection;

        }


    }
}