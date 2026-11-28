using System;
using System.Collections.Generic;

namespace SatelliteFixer.Core
{
    /// <summary>
    /// The synchronized game state. The PC app (host) owns it, the VR app (client) receives copies.
    /// Only plain serializable data, so it can be sent over the network and saved with JsonUtility.
    /// </summary>
    [Serializable]
    public class GameState
    {
        /// <summary>Incremented on every change, so peers can ignore outdated updates.</summary>
        public int revision;

        public ScenarioPhase phase = ScenarioPhase.Lobby;

        /// <summary>ID of the current <see cref="SatelliteDefinition"/>, empty in the lobby.</summary>
        public string satelliteId = string.Empty;

        public List<FaultInstance> faults = new List<FaultInstance>();

        /// <summary>True if every fault of the current scenario has been repaired.</summary>
        public bool AllFaultsRepaired
        {
            get
            {
                foreach (var fault in faults)
                {
                    if (fault.status != FaultStatus.Repaired)
                        return false;
                }

                return true;
            }
        }
    }

    /// <summary>
    /// A fault that is active in the current scenario.
    /// </summary>
    [Serializable]
    public class FaultInstance
    {
        public string faultId;

        /// <summary>Copied from the fault definition so the VR app can map faults to sensors without a lookup.</summary>
        public string sensorId;

        public FaultStatus status;
    }

    /// <summary>
    /// A repair action the astronaut performed on a sensor. Sent from the VR app to the host,
    /// which checks it against the active faults.
    /// </summary>
    [Serializable]
    public struct RepairAttempt
    {
        public string sensorId;
        public RepairAction action;
    }

    public enum ScenarioPhase
    {
        Lobby,
        Repair,
        Completed,
    }

    public enum FaultStatus
    {
        Active,
        Repaired,
    }

    /// <summary>
    /// What the astronaut can do to a sensor. Placeholder set; extend it together with the VR interactions (AP 4.3).
    /// </summary>
    public enum RepairAction
    {
        Inspect,
        Reconnect,
        Replace,
        Realign,
        Reset,
    }
}
