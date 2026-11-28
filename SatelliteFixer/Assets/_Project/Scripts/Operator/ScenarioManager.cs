using System;
using System.Collections.Generic;
using SatelliteFixer.Core;
using UnityEngine;

namespace SatelliteFixer.Operator
{
    /// <summary>
    /// Runs the missions on the PC app (AP 5.1): triggers random faults on the chosen satellite
    /// and checks the repair actions the astronaut sends.
    /// </summary>
    public class ScenarioManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Game state owner that this manager writes to.")]
        GameStateManager m_GameStateManager;

        [SerializeField, Tooltip("Satellites available as missions, in the order shown in the lobby.")]
        List<SatelliteDefinition> m_Satellites = new List<SatelliteDefinition>();

        /// <summary>
        /// Raised after a repair attempt was checked. The bool is true if it fixed a fault.
        /// </summary>
        public event Action<RepairAttempt, bool> RepairChecked;

        /// <summary>
        /// The scenario manager of the running session, for scene objects that cannot reference it directly.
        /// </summary>
        public static ScenarioManager Instance { get; private set; }

        public IReadOnlyList<SatelliteDefinition> Satellites => m_Satellites;

        /// <summary>
        /// Satellite of the running mission, or null in the lobby.
        /// Derived from the game state, so it is also correct after loading a saved state.
        /// </summary>
        public SatelliteDefinition CurrentSatellite => FindSatellite(m_GameStateManager.State.satelliteId);

        void OnEnable()
        {
            Instance = this;
            if (m_GameStateManager != null && m_GameStateManager.Sync != null)
                m_GameStateManager.Sync.RepairAttemptReceived += OnRepairAttemptReceived;
        }

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;

            if (m_GameStateManager != null && m_GameStateManager.Sync != null)
                m_GameStateManager.Sync.RepairAttemptReceived -= OnRepairAttemptReceived;
        }

        /// <summary>
        /// Rolls the faults for the given satellite and switches the game state to <see cref="ScenarioPhase.Mission"/>.
        /// </summary>
        public void BeginMission(string satelliteId, int faultCount)
        {
            var satellite = FindSatellite(satelliteId);
            if (satellite == null)
            {
                Debug.LogWarning($"Unknown satellite '{satelliteId}', starting the mission without faults.", this);
            }

            var faults = satellite != null ? PickRandomFaults(satellite, faultCount) : new List<FaultDefinition>();

            m_GameStateManager.Modify(state =>
            {
                state.phase = ScenarioPhase.Mission;
                state.satelliteId = satelliteId;
                state.missionTime = 0f;
                state.paused = false;
                state.faults.Clear();
                foreach (var fault in faults)
                {
                    state.faults.Add(new FaultInstance
                    {
                        faultId = fault.Id,
                        sensorId = fault.AffectedSensor != null ? fault.AffectedSensor.Id : string.Empty,
                        status = FaultStatus.Active,
                    });
                }
            });
        }

        public SatelliteDefinition FindSatellite(string satelliteId)
        {
            if (string.IsNullOrEmpty(satelliteId))
                return null;

            return m_Satellites.Find(satellite => satellite != null && satellite.Id == satelliteId);
        }

        static List<FaultDefinition> PickRandomFaults(SatelliteDefinition satellite, int count)
        {
            var candidates = new List<FaultDefinition>();
            foreach (var fault in satellite.PossibleFaults)
            {
                if (fault != null)
                    candidates.Add(fault);
            }

            // Partial Fisher-Yates shuffle: the first `count` entries end up random and distinct.
            count = Mathf.Clamp(count, 0, candidates.Count);
            for (var i = 0; i < count; i++)
            {
                var j = UnityEngine.Random.Range(i, candidates.Count);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            return candidates.GetRange(0, count);
        }

        void OnRepairAttemptReceived(RepairAttempt attempt)
        {
            var state = m_GameStateManager.State;
            var satellite = CurrentSatellite;
            if (state.phase != ScenarioPhase.Mission || satellite == null)
                return;

            var faultIndex = state.faults.FindIndex(fault =>
                fault.status == FaultStatus.Active
                && fault.sensorId == attempt.sensorId
                && satellite.FindFault(fault.faultId)?.RequiredAction == attempt.action);

            if (faultIndex >= 0)
            {
                m_GameStateManager.Modify(s =>
                {
                    s.faults[faultIndex].status = FaultStatus.Repaired;
                    if (s.AllFaultsRepaired)
                        s.phase = ScenarioPhase.Debrief;
                });
            }

            RepairChecked?.Invoke(attempt, faultIndex >= 0);
        }
    }
}
