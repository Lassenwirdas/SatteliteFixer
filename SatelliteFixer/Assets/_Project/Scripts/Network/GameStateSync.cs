using System;
using SatelliteFixer.Core;
using UnityEngine;

namespace SatelliteFixer.Network
{
    /// <summary>
    /// Transport-independent game state exchange between the PC app (host) and the VR app (client).
    /// Gameplay scripts only talk to this class, so the transport (local loopback for testing,
    /// Netcode for the real setup, AP 3) can be swapped without touching gameplay code.
    /// </summary>
    public abstract class GameStateSync : MonoBehaviour
    {
        /// <summary>
        /// The sync of the running session. Scene objects use it when they cannot reference the
        /// session object directly, because it lives in DontDestroyOnLoad.
        /// </summary>
        public static GameStateSync Active { get; private set; }

        /// <summary>
        /// Raised on every peer when a new game state arrives. On the host it is raised right after publishing.
        /// </summary>
        public event Action<GameState> StateReceived;

        /// <summary>
        /// Raised on the host when the astronaut performs a repair action.
        /// </summary>
        public event Action<RepairAttempt> RepairAttemptReceived;

        /// <summary>
        /// Raised on the host when the astronaut has loaded its mission scene.
        /// </summary>
        public event Action ClientReadyReceived;

        /// <summary>
        /// Raised on the host when the astronaut connects (true) or disconnects (false),
        /// and on the client when its own connection to the host comes up or goes down.
        /// </summary>
        public event Action<bool> PeerConnectionChanged;

        /// <summary>
        /// True on the PC app, which owns the game state.
        /// </summary>
        public abstract bool IsHost { get; }

        /// <summary>
        /// True while the other peer is connected.
        /// </summary>
        public bool IsPeerConnected { get; private set; }

        /// <summary>
        /// The most recent game state this peer knows about, or null before the first update.
        /// </summary>
        public GameState LatestState { get; private set; }

        /// <summary>
        /// Sends the authoritative game state to all peers. Host only.
        /// </summary>
        public abstract void PublishState(GameState state);

        /// <summary>
        /// Sends a repair action to the host. Client only (the loopback also allows it on the host).
        /// </summary>
        public abstract void SendRepairAttempt(RepairAttempt attempt);

        /// <summary>
        /// Tells the host that the mission scene is loaded. Client only (the loopback also allows it on the host).
        /// </summary>
        public abstract void SendClientReady();

        protected virtual void OnEnable()
        {
            Active = this;
        }

        protected virtual void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        /// <summary>
        /// Called by implementations when a game state arrived. Drops updates older than the current one.
        /// </summary>
        protected void OnStateReceived(GameState state)
        {
            if (state == null || (LatestState != null && state.revision < LatestState.revision))
                return;

            LatestState = state;
            StateReceived?.Invoke(state);
        }

        /// <summary>
        /// Forgets the last received state, e.g. after the client lost the connection to a host that restarted.
        /// </summary>
        protected void ResetLatestState()
        {
            LatestState = null;
        }

        /// <summary>
        /// Called by implementations when a repair attempt arrived on the host.
        /// </summary>
        protected void OnRepairAttemptReceived(RepairAttempt attempt)
        {
            RepairAttemptReceived?.Invoke(attempt);
        }

        /// <summary>
        /// Called by implementations when the client reported ready on the host.
        /// </summary>
        protected void OnClientReadyReceived()
        {
            ClientReadyReceived?.Invoke();
        }

        /// <summary>
        /// Called by implementations when the connection to the other peer changes.
        /// </summary>
        protected void OnPeerConnectionChanged(bool connected)
        {
            if (connected == IsPeerConnected)
                return;

            IsPeerConnected = connected;
            PeerConnectionChanged?.Invoke(connected);
        }
    }
}
