using SatelliteFixer.Core;
using UnityEngine;

namespace SatelliteFixer.Network
{
    /// <summary>
    /// Loopback implementation of <see cref="GameStateSync"/> for testing operator and astronaut
    /// in a single scene (e.g. in the editor with the XR Device Simulator). No network involved.
    /// States are copied through JSON, so the gameplay code sees the same serialized data as over the network.
    /// </summary>
    public class LocalGameStateSync : GameStateSync
    {
        public override bool IsHost => true;

        public override void PublishState(GameState state)
        {
            OnStateReceived(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state)));
        }

        public override void SendRepairAttempt(RepairAttempt attempt)
        {
            OnRepairAttemptReceived(attempt);
        }
    }
}
