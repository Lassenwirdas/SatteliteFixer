using System.Collections.Generic;
using UnityEngine;

namespace SatelliteFixer.Core
{
    /// <summary>
    /// A real ESA satellite used as a game level, with its sensors and the faults that can occur on it.
    /// 3D models come from ESA SciFleet (https://scifleet.esa.int/).
    /// </summary>
    [CreateAssetMenu(menuName = "SatelliteFixer/Satellite", fileName = "Satellite")]
    public class SatelliteDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable ID used in the synchronized game state. Do not change it once in use.")]
        string m_Id;

        [SerializeField, Tooltip("Name shown in the UI.")]
        string m_DisplayName;

        [SerializeField, Tooltip("ESA mission or programme the satellite belongs to.")]
        string m_Mission;

        [SerializeField, TextArea(3, 10), Tooltip("Handbook entry about the mission and what the satellite is used for.")]
        string m_HandbookText;

        [SerializeField, Tooltip("Satellite model (from ESA SciFleet) spawned in the VR scene.")]
        GameObject m_ModelPrefab;

        [SerializeField, Tooltip("Sensors mounted on this satellite.")]
        List<SensorDefinition> m_Sensors = new List<SensorDefinition>();

        [SerializeField, Tooltip("Faults the scenario manager can pick from for this satellite.")]
        List<FaultDefinition> m_PossibleFaults = new List<FaultDefinition>();

        public string Id => m_Id;
        public string DisplayName => m_DisplayName;
        public string Mission => m_Mission;
        public string HandbookText => m_HandbookText;
        public GameObject ModelPrefab => m_ModelPrefab;
        public IReadOnlyList<SensorDefinition> Sensors => m_Sensors;
        public IReadOnlyList<FaultDefinition> PossibleFaults => m_PossibleFaults;

        /// <summary>
        /// Looks up a fault of this satellite by its ID, or returns null.
        /// </summary>
        public FaultDefinition FindFault(string faultId)
        {
            foreach (var fault in m_PossibleFaults)
            {
                if (fault != null && fault.Id == faultId)
                    return fault;
            }

            return null;
        }
    }
}
