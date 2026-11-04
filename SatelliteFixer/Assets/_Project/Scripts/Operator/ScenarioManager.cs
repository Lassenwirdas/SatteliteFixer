using System;
using System.Collections.Generic;
using SatelliteFixer.Core;
using UnityEngine;

namespace SatelliteFixer.Operator
{
    /// <summary>
    /// Runs the scenarios on the PC app (AP 5.1): picks a satellite, triggers random faults
    /// and checks the repair actions the astronaut sends.
    /// </summary>
    public class ScenarioManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Game state owner that this manager writes to.")]
        GameStateManager m_GameStateManager;

        [SerializeField, Tooltip("Satellites available as scenarios, in play order.")]
        List<SatelliteDefinition> m_Satellites = new List<SatelliteDefinition>();

        [SerializeField, Min(1), Tooltip("How many faults are triggered per scenario (capped by the satellite's possible faults).")]
        int m_FaultsPerScenario = 1;

        /// <summary>
        /// Raised after a repair attempt was checked. The bool is true if it fixed a fault.
        /// </summary>
        public event Action<RepairAttempt, bool> RepairChecked;

        public IReadOnlyList<SatelliteDefinition> Satellites => m_Satellites;

        /// <summary>
        /// Satellite of the running scenario, or null in the lobby.
        /// Derived from the game state, so it is also correct after loading a saved state.
        /// </summary>
        public SatelliteDefinition CurrentSatellite => FindSatellite(m_GameStateManager.State.satelliteId);

        void OnEnable()
        {
            if (m_GameStateManager != null && m_GameStateManager.Sync != null)
                m_GameStateManager.Sync.RepairAttemptReceived += OnRepairAttemptReceived;
        }

        void OnDisable()
        {
            if (m_GameStateManager != null && m_GameStateManager.Sync != null)
                m_GameStateManager.Sync.RepairAttemptReceived -= OnRepairAttemptReceived;
        }

        /// <summary>
        /// Starts the scenario for the satellite at the given index. Can be wired to a UI button.
        /// </summary>
        public void StartScenario(int satelliteIndex)
        {
            if (satelliteIndex < 0 || satelliteIndex >= m_Satellites.Count || m_Satellites[satelliteIndex] == null)
            {
                Debug.LogWarning($"No satellite at index {satelliteIndex}.", this);
                return;
            }

            var satellite = m_Satellites[satelliteIndex];
            var faults = PickRandomFaults(satellite, m_FaultsPerScenario);

            m_GameStateManager.Modify(state =>
            {
                state.phase = ScenarioPhase.Repair;
                state.satelliteId = satellite.Id;
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

        public void StartRandomScenario()
        {
            StartScenario(UnityEngine.Random.Range(0, m_Satellites.Count));
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
            count = Mathf.Min(count, candidates.Count);
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
            if (state.phase != ScenarioPhase.Repair || satellite == null)
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
                        s.phase = ScenarioPhase.Completed;
                });
            }

            RepairChecked?.Invoke(attempt, faultIndex >= 0);
        }
    }
}
