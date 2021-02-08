using System;
using UnityEngine;

namespace Mirror.Momentum
{
    internal class MovementSync : NetworkBehaviour, IComparable<MovementSync>
    {
        [Tooltip("Check if this object will be moved by a player,  uncheck if only the server moves this object")]
        public bool PlayerControlled;

        public int CompareTo(MovementSync other)
        {
            return NetId.CompareTo(other.NetId);
        }
    }
}