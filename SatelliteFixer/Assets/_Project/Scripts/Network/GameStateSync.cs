using System;
using SatelliteFixer.Core;
using UnityEngine;

namespace SatelliteFixer.Network
{
    /// <summary>
    /// Transport-independent game state exchange between the PC app (host) and the VR app (client).
    /// Gameplay scripts only talk to this class, so the transport (local loopback for testing,
    /// Netcode or similar for the real setup, AP 3) can be swapped without touching gameplay code.
    /// </summary>
    public abstract class GameStateSync : MonoBehaviour
    {
        /// <summary>
        /// Raised on every peer when a new game state arrives. On the host it is raised right after publishing.
        /// </summary>
        public event Action<GameState> StateReceived;

        /// <summary>
        /// Raised on the host when the astronaut performs a repair action.
        /// </summary>
        public event Action<RepairAttempt> RepairAttemptReceived;

        /// <summary>
        /// True on the PC app, which owns the game state.
        /// </summary>
        public abstract bool IsHost { get; }

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
        /// Called by implementations when a repair attempt arrived on the host.
        /// </summary>
        protected void OnRepairAttemptReceived(RepairAttempt attempt)
        {
            RepairAttemptReceived?.Invoke(attempt);
        }
    }
}
