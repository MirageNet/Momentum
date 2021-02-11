using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Mirror.Momentum.Snapshot;

namespace Mirror.Momentum
{

    /// <summary>
    /// The snapshot system generates snapshots of all objects in the server
    /// sends them to the clients
    /// and performs interpolation on the clients
    /// </summary>
    public class MovementSystem : MonoBehaviour
    {
        public int SnapshotPerSecond = 30;

        public ClientObjectManager ClientObjectManager;
        public ServerObjectManager ServerObjectManager;

        private readonly SortedSet<MovementSync> objects = new SortedSet<MovementSync>();

        public void Awake()
        {
            InitServer();
            InitClient();
        }

        private void InitServer()
        {
            ServerObjectManager.Spawned.AddListener(Spawned);
            ServerObjectManager.UnSpawned.AddListener(UnSpawned);

            ServerObjectManager.Server.Started.AddListener(OnStartServer);
            ServerObjectManager.Server.Stopped.AddListener(OnStopServer);
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
                yield return new WaitForSeconds(1f / SnapshotPerSecond);
            }
        }

        private void SendSnapshot()
        {
            // generate a snapshot of all objects and send it to the clients

            Snapshot snapshot = TakeSnapshot();

            foreach (INetworkConnection connection in ServerObjectManager.Server.connections)
            {
                if (connection.IsReady)
                    connection.SendNotify(snapshot, null);
            }
        }

        private Snapshot TakeSnapshot()
        {
            var snapshot = new Snapshot()
            {
                Time = Time.unscaledTime
            };

            foreach (MovementSync obj in objects)
            {
                var objectState = new ObjectState()
                {
                    NetId = obj.NetId,
                    Position = obj.transform.position,
                    Rotation = obj.transform.rotation,
                };
                snapshot.ObjectsState.Add(objectState);
            }
            return snapshot;
        }

        #endregion;

        #region Client

        ExponentialMovingAverage clientTimeOffsetAvg;
        ExponentialMovingAverage clientSnapshotDeliveryInterval;
        float? clientLastSnapshotTime;

        double clientInterpolationTime;

        const int SNAPSHOT_OFFSET_COUNT = 2;
        private float interpolationOffset;
        private float iterpolationTimeOffsetAheadThreshold;
        private float iterpolationTimeOffsetBehindThreshold;
        private float clientInterpolationTimeScale = 1.0f;

        private void InitClient()
        {
            ClientObjectManager.Spawned.AddListener(Spawned);
            ClientObjectManager.UnSpawned.AddListener(UnSpawned);

            ClientObjectManager.Client.Authenticated.AddListener(OnClientConnected);
            clientTimeOffsetAvg = new ExponentialMovingAverage(SnapshotPerSecond);
            interpolationOffset = (float)SNAPSHOT_OFFSET_COUNT / SnapshotPerSecond;

            iterpolationTimeOffsetAheadThreshold = 1f / SnapshotPerSecond;
            iterpolationTimeOffsetBehindThreshold = -0.5f / SnapshotPerSecond;
            clientSnapshotDeliveryInterval = new ExponentialMovingAverage(SnapshotPerSecond);

            clientLastSnapshotTime = null;
        }

        private void OnClientConnected(INetworkConnection connection)
        {
            connection.RegisterHandler<Snapshot>(OnReceiveSnapshot);
        }

        private readonly List<Snapshot> snapshots = new List<Snapshot>();

        // we will interpolate from this snapshot to the next snapshot

        private void OnReceiveSnapshot(INetworkConnection arg1, Snapshot snapshot)
        {
            // ignore messages in host mode
            if (ClientObjectManager.Client.IsLocalClient)
                return;

            // first snapshot
            if (snapshots.Count == 0)
            {
                clientInterpolationTime = snapshot.Time - interpolationOffset;
            }

            snapshots.Add(snapshot);

            if (clientLastSnapshotTime.HasValue)
            {
                clientSnapshotDeliveryInterval.Add(Time.time - clientLastSnapshotTime.Value);
                interpolationOffset = (float)(SNAPSHOT_OFFSET_COUNT * clientSnapshotDeliveryInterval.Value);
            }

            clientLastSnapshotTime = Time.time;

            var diff = snapshot.Time - clientInterpolationTime;

            clientTimeOffsetAvg.Add(diff);

            var diffWanted = clientTimeOffsetAvg.Value - interpolationOffset;

            if (diffWanted > iterpolationTimeOffsetAheadThreshold)
            {
                clientInterpolationTimeScale = 1.01f;
            }
            else if (diffWanted < iterpolationTimeOffsetBehindThreshold)
            {
                clientInterpolationTimeScale = 0.99f;
            }
            else
            {
                clientInterpolationTimeScale = 1.0f;
            }

        }

        public void Update()
        {
            if (ClientObjectManager.Client.IsConnected && !ClientObjectManager.Client.IsLocalClient)
            {
                InterpolateObjects();
            }
        }

        private void InterpolateObjects()
        {
            if (snapshots.Count == 0)
                return;

            clientInterpolationTime += Time.unscaledDeltaTime * clientInterpolationTimeScale;

            float alpha = 0;

            Snapshot from = default;
            Snapshot to = default;
            int removeCount = 0;

            for (int i = 0; i< snapshots.Count; i++)
            {
                from = snapshots[i];

                if (i + 1 == snapshots.Count)
                {
                    to = from;
                    alpha = 0;
                }
                else
                {
                    int f = i;
                    int t = i + 1;

                    if (snapshots[f].Time <= clientInterpolationTime && snapshots[t].Time > clientInterpolationTime)
                    {
                        from = snapshots[f];
                        to = snapshots[t];

                        alpha = Mathf.InverseLerp((float)from.Time, (float)to.Time, (float)clientInterpolationTime);
                        break;
                    }
                    else if (snapshots[t].Time <= clientInterpolationTime)
                    {
                        removeCount++;
                    }
                }
            }

            snapshots.RemoveRange(0, removeCount);

            MoveObjects(from, to, alpha);
        }

        private void MoveObjects(Snapshot from, Snapshot to, float alpha)
        {
            var fromEnumerator = from.ObjectsState.GetEnumerator();
            var toEnumerator = to.ObjectsState.GetEnumerator();
            var objEnumerator = objects.GetEnumerator();

            if (!fromEnumerator.MoveNext())
                return;

            if (!toEnumerator.MoveNext())
                return;

            while (objEnumerator.MoveNext())
            {
                // get the from matching the object
                uint netId = objEnumerator.Current.NetId;

                while (fromEnumerator.Current.NetId < netId)
                {
                    // nothing else to interpolate from;
                    if (!fromEnumerator.MoveNext())
                        return;
                }

                while (toEnumerator.Current.NetId < netId)
                {
                    if (!toEnumerator.MoveNext())
                        return;
                }

                ObjectState objFrom = fromEnumerator.Current;
                ObjectState objTo = toEnumerator.Current;

                if (objFrom.NetId == netId || objTo.NetId == netId)
                {
                    MoveObject(objEnumerator.Current, objFrom, objTo, alpha);
                }
            }
        }

        /// <summary>
        /// Interpolate the object from state to state with a given alpha
        /// </summary>
        /// <param name="obj">The object to move</param>
        /// <param name="from">the from state</param>
        /// <param name="to">the to state</param>
        /// <param name="alpha">the interpolation alpha</param>
        private void MoveObject(MovementSync obj, ObjectState from, ObjectState to, float alpha)
        {
            if (obj.PlayerControlled && obj.HasAuthority)
                return;

            Transform tr = obj.transform;

            tr.position = Vector3.Lerp(from.Position, to.Position, alpha);
            tr.rotation = Quaternion.Slerp(from.Rotation, to.Rotation, alpha);
        }


        #endregion
    }

}