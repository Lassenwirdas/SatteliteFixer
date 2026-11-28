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

        /// <summary>Drives the scene flow on both peers: Lobby → Loading → Mission → Debrief.</summary>
        public ScenarioPhase phase = ScenarioPhase.Lobby;

        /// <summary>Settings chosen in the lobby for the next or current mission.</summary>
        public MissionSettings settings = new MissionSettings();

        /// <summary>ID of the <see cref="SatelliteDefinition"/> of the running mission, empty in the lobby.</summary>
        public string satelliteId = string.Empty;

        public List<FaultInstance> faults = new List<FaultInstance>();

        /// <summary>True while the VR app is connected to the host.</summary>
        public bool astronautConnected;

        /// <summary>True once the VR app has loaded its mission scene (set during <see cref="ScenarioPhase.Loading"/>).</summary>
        public bool astronautReady;

        /// <summary>Mission time in seconds. Only runs in <see cref="ScenarioPhase.Mission"/> while not paused.</summary>
        public float missionTime;

        /// <summary>True while the mission clock is stopped, e.g. because the astronaut lost the connection.</summary>
        public bool paused;

        /// <summary>True if every fault of the current mission has been repaired.</summary>
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
    /// What the operator picks in the lobby (or what a preset ScriptableObject provides).
    /// </summary>
    [Serializable]
    public class MissionSettings
    {
        public string satelliteId = string.Empty;
        public bool tutorial;
        public int faultCount = 2;
    }

    /// <summary>
    /// A fault that is active in the current mission.
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
        /// <summary>Both apps are in the lobby scene; the operator picks the mission.</summary>
        Lobby,

        /// <summary>Both apps load their role scene; the host waits for the astronaut to report ready.</summary>
        Loading,

        /// <summary>The mission runs and the clock ticks.</summary>
        Mission,

        /// <summary>All faults repaired or time is up; both apps show the results.</summary>
        Debrief,
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
