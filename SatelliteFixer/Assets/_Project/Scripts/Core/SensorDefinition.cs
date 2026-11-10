using UnityEngine;

namespace SatelliteFixer.Core
{
    /// <summary>
    /// A sensor or component type that can be mounted on a satellite (e.g. star tracker, magnetometer).
    /// Shared by both apps: the operator sees its data and handbook entry, the astronaut sees and repairs its model.
    /// </summary>
    [CreateAssetMenu(menuName = "SatelliteFixer/Sensor", fileName = "Sensor")]
    public class SensorDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable ID used in the synchronized game state. Do not change it once in use.")]
        string m_Id;

        [SerializeField, Tooltip("Name shown in the UI and the handbook.")]
        string m_DisplayName;

        [SerializeField, TextArea(3, 10), Tooltip("Handbook entry: what the sensor does and how it works.")]
        string m_HandbookText;

        [SerializeField, Tooltip("Normal value range shown in the operator's sensor data view.")]
        Vector2 m_NominalRange;

        [SerializeField, Tooltip("Unit of the sensor reading, e.g. V, °C, °/s.")]
        string m_Unit;

        public string Id => m_Id;
        public string DisplayName => m_DisplayName;
        public string HandbookText => m_HandbookText;
        public Vector2 NominalRange => m_NominalRange;
        public string Unit => m_Unit;
    }
}
