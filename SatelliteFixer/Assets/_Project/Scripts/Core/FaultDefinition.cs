using UnityEngine;

namespace SatelliteFixer.Core
{
    /// <summary>
    /// A fault that the scenario manager can trigger on a sensor, plus what the operator sees
    /// and which action the astronaut has to perform to fix it.
    /// </summary>
    [CreateAssetMenu(menuName = "SatelliteFixer/Fault", fileName = "Fault")]
    public class FaultDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable ID used in the synchronized game state. Do not change it once in use.")]
        string m_Id;

        [SerializeField, Tooltip("Name shown in the UI.")]
        string m_DisplayName;

        [SerializeField, Tooltip("The sensor this fault affects.")]
        SensorDefinition m_AffectedSensor;

        [SerializeField, TextArea(2, 6), Tooltip("Symptom shown to the operator, e.g. in the sensor data view.")]
        string m_OperatorSymptom;

        [SerializeField, TextArea(2, 6), Tooltip("Repair hint from the handbook that the operator reads out to the astronaut.")]
        string m_RepairInstruction;

        [SerializeField, Tooltip("Action the astronaut has to perform on the affected sensor.")]
        RepairAction m_RequiredAction;

        public string Id => m_Id;
        public string DisplayName => m_DisplayName;
        public SensorDefinition AffectedSensor => m_AffectedSensor;
        public string OperatorSymptom => m_OperatorSymptom;
        public string RepairInstruction => m_RepairInstruction;
        public RepairAction RequiredAction => m_RequiredAction;
    }
}
