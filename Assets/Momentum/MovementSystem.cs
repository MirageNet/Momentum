using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror.Momentum
{

    /// <summary>
    /// The snapshot system generates snapshots of all objects in the server
    /// sends them to the clients
    /// and performs interpolation on the clients
    /// </summary>
    public class MovementSystem : MonoBehaviour
    {
        public float ticksPerSecond = 30;

        public ClientObjectManager ClientObjectManager;
        public ServerObjectManager ServerObjectManager;

        private readonly SortedSet<MovementSync> objects = new SortedSet<MovementSync>();

        public void Awake()
        {
            ClientObjectManager.Spawned.AddListener(Spawned);
            ClientObjectManager.UnSpawned.AddListener(UnSpawned);
            ServerObjectManager.Spawned.AddListener(Spawned);
            ServerObjectManager.UnSpawned.AddListener(UnSpawned);

            ServerObjectManager.Server.Started.AddListener(OnStartServer);
            ServerObjectManager.Server.Stopped.AddListener(OnStopServer);

            ClientObjectManager.Client.Authenticated.AddListener(OnClientConnected);
        }

        private void Spawned(NetworkIdentity ni)
        {
            if (ni.TryGetComponent(out MovementSync movementSync))
            {
                objects.Add(movementSync);
            }
        }

        private void UnSpawned(NetworkIdentity ni)
        {
            if (ni.TryGetComponent(out MovementSync movementSync))
            {
                objects.Remove(movementSync);
            }
        }

        #region Server generating and sending snapshots
        private void OnStartServer()
        {
            StartCoroutine(SendSnapshots());
        }

        private void OnStopServer()
        {
            StopAllCoroutines();
        }

        private IEnumerator SendSnapshots()
        {
            while (true)
            {
                SendSnapshot();
                yield return new WaitForSeconds(1f / ticksPerSecond);
            }
        }

        Sequencer snapshotSequencer = new Sequencer(8);

        private void SendSnapshot()
        {
            // generate a snapshot of all objects and send it to the clients

            Snapshot snapshot = TakeSnapshot();

            foreach (INetworkConnection connection in ServerObjectManager.Server.connections)
            {
                connection.SendNotify(snapshot, null);
            }
        }

        private Snapshot TakeSnapshot()
        {
            Snapshot snapshot = new Snapshot()
            {
                time = Time.time
            };

            foreach (var obj in objects)
            {
                var objectState = new Snapshot.ObjectState()
                {
                    NetId = obj.NetId,
                    Position = obj.transform.position,
                    Rotation = obj.transform.rotation,
                };
                snapshot.currentState.Add(objectState);
            }
            return snapshot;
        }

        #endregion;

        #region Client
        private void OnClientConnected(INetworkConnection connection)
        {
            connection.RegisterHandler<Snapshot>(OnReceiveSnapshot);
        }

        private readonly Queue<Snapshot> snapshots = new Queue<Snapshot>();

        // we will interpolate from this snapshot to the next snapshot
        private Snapshot prev;

        private void OnReceiveSnapshot(INetworkConnection arg1, Snapshot snapshot)
        {
            snapshots.Enqueue(snapshot);            
        }


        #endregion
    }

}