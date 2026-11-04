using System.Text;
using SatelliteFixer.Core;
using TMPro;
using UnityEngine;

namespace SatelliteFixer.Operator
{
    /// <summary>
    /// Minimal mission control display: current satellite, phase and the symptoms of the active faults.
    /// Placeholder for the sensor data visualization (AP 1.1).
    /// </summary>
    public class OperatorStatusView : MonoBehaviour
    {
        [SerializeField, Tooltip("Game state owner to read from.")]
        GameStateManager m_GameStateManager;

        [SerializeField, Tooltip("Scenario manager that knows the satellites.")]
        ScenarioManager m_ScenarioManager;

        [SerializeField, Tooltip("Text the status is written to.")]
        TMP_Text m_Text;

        void OnEnable()
        {
            if (m_GameStateManager != null && m_GameStateManager.Sync != null)
                m_GameStateManager.Sync.StateReceived += Refresh;
        }

        void OnDisable()
        {
            if (m_GameStateManager != null && m_GameStateManager.Sync != null)
                m_GameStateManager.Sync.StateReceived -= Refresh;
        }

        void Refresh(GameState state)
        {
            var satellite = m_ScenarioManager.FindSatellite(state.satelliteId);
            var text = new StringBuilder();

            if (state.phase == ScenarioPhase.Lobby || satellite == null)
            {
                text.AppendLine("Waiting for a scenario to start.");
            }
            else
            {
                text.AppendLine($"{satellite.DisplayName} ({satellite.Mission})");
                text.AppendLine(state.phase == ScenarioPhase.Completed ? "All systems nominal." : "Fault detected.");
                text.AppendLine();

                foreach (var fault in state.faults)
                {
                    var definition = satellite.FindFault(fault.faultId);
                    var sensorName = definition != null && definition.AffectedSensor != null ? definition.AffectedSensor.DisplayName : fault.sensorId;
                    var symptom = definition != null ? definition.OperatorSymptom : string.Empty;
                    var status = fault.status == FaultStatus.Repaired ? "OK" : "ERROR";
                    text.AppendLine($"[{status}] {sensorName}: {symptom}");
                }
            }

            m_Text.text = text.ToString();
        }
    }
}
