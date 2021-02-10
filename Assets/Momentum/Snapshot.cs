using System.Collections.Generic;
using UnityEngine;

namespace Mirror.Momentum
{

 
    internal class Snapshot
    {
        public struct ObjectState
        {
            public uint NetId;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        // server time when this snapshot got generated
        public double Time;

        public List<ObjectState> ObjectsState = new List<ObjectState>();

    }
}