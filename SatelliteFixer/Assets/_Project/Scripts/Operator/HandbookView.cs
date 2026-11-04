using System.Text;
using TMPro;
using UnityEngine;

namespace SatelliteFixer.Operator
{
    /// <summary>
    /// Minimal digital handbook for the operator (AP 1.2): mission info, sensors and repair instructions
    /// of a satellite. The operator has to match the symptoms to the right entry and read it out.
    /// </summary>
    public class HandbookView : MonoBehaviour
    {
        [SerializeField, Tooltip("Scenario manager that lists the available satellites.")]
        ScenarioManager m_ScenarioManager;

        [SerializeField, Tooltip("Text the handbook page is written to.")]
        TMP_Text m_Text;

        /// <summary>
        /// Shows the handbook page of the satellite at the given index. Can be wired to a UI button.
        /// </summary>
        public void ShowSatellite(int satelliteIndex)
        {
            var satellites = m_ScenarioManager.Satellites;
            if (satelliteIndex < 0 || satelliteIndex >= satellites.Count || satellites[satelliteIndex] == null)
                return;

            var satellite = satellites[satelliteIndex];
            var text = new StringBuilder();
            text.AppendLine($"<b>{satellite.DisplayName}</b> ({satellite.Mission})");
            text.AppendLine(satellite.HandbookText);

            text.AppendLine();
            text.AppendLine("<b>Sensors</b>");
            foreach (var sensor in satellite.Sensors)
            {
                if (sensor == null)
                    continue;

                text.AppendLine($"<b>{sensor.DisplayName}</b> (nominal {sensor.NominalRange.x} to {sensor.NominalRange.y} {sensor.Unit})");
                text.AppendLine(sensor.HandbookText);
            }

            text.AppendLine();
            text.AppendLine("<b>Troubleshooting</b>");
            foreach (var fault in satellite.PossibleFaults)
            {
                if (fault == null)
                    continue;

                text.AppendLine($"<b>{fault.DisplayName}</b>: {fault.OperatorSymptom}");
                text.AppendLine(fault.RepairInstruction);
            }

            m_Text.text = text.ToString();
        }
    }
}
