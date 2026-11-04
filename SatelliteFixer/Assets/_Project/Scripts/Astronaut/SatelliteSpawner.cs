using System.Collections.Generic;
using SatelliteFixer.Core;
using SatelliteFixer.Network;
using UnityEngine;

namespace SatelliteFixer.Astronaut
{
    /// <summary>
    /// Spawns the model of the current satellite in the VR scene whenever the scenario changes,
    /// and connects the <see cref="RepairableSensor"/> components on it to the game state sync.
    /// </summary>
    public class SatelliteSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("Sync to receive the game state from the host.")]
        GameStateSync m_Sync;

        [SerializeField, Tooltip("All satellites that can be played, to look up the model by ID.")]
        List<SatelliteDefinition> m_Satellites = new List<SatelliteDefinition>();

        [SerializeField, Tooltip("Where the satellite model is placed. Defaults to this transform.")]
        Transform m_Anchor;

        GameObject m_SpawnedModel;
        string m_SpawnedSatelliteId = string.Empty;

        void OnEnable()
        {
            if (m_Sync == null)
                return;

            m_Sync.StateReceived += OnStateReceived;
            if (m_Sync.LatestState != null)
                OnStateReceived(m_Sync.LatestState);
        }

        void OnDisable()
        {
            if (m_Sync != null)
                m_Sync.StateReceived -= OnStateReceived;
        }

        void OnStateReceived(GameState state)
        {
            if (state.satelliteId == m_SpawnedSatelliteId)
                return;

            if (m_SpawnedModel != null)
                Destroy(m_SpawnedModel);

            m_SpawnedModel = null;
            m_SpawnedSatelliteId = state.satelliteId;

            var satellite = m_Satellites.Find(s => s != null && s.Id == state.satelliteId);
            if (satellite == null || satellite.ModelPrefab == null)
                return;

            var anchor = m_Anchor != null ? m_Anchor : transform;
            m_SpawnedModel = Instantiate(satellite.ModelPrefab, anchor);
            foreach (var sensor in m_SpawnedModel.GetComponentsInChildren<RepairableSensor>(true))
                sensor.Initialize(m_Sync);
        }
    }
}
