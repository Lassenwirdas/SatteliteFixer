using System.Collections.Generic;
using SatelliteFixer.Core;
using SatelliteFixer.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SatelliteFixer.Operator
{
    /// <summary>
    /// Lobby screen of the PC app: shows the host address and the astronaut's connection,
    /// and lets the operator pick satellite, tutorial and fault count before starting the mission.
    /// </summary>
    public class OperatorLobbyView : MonoBehaviour
    {
        [SerializeField, Tooltip("Connection status of the astronaut.")]
        TMP_Text m_StatusText;

        [SerializeField, Tooltip("IP addresses of this PC, for a manual connection from the Quest.")]
        TMP_Text m_AddressText;

        [SerializeField, Tooltip("Satellite selection.")]
        TMP_Dropdown m_SatelliteDropdown;

        [SerializeField, Tooltip("Tutorial on/off.")]
        Toggle m_TutorialToggle;

        [SerializeField, Tooltip("Number of faults (whole numbers).")]
        Slider m_FaultCountSlider;

        [SerializeField, Tooltip("Shows the fault count next to the slider.")]
        TMP_Text m_FaultCountText;

        [SerializeField, Tooltip("Starts the mission once the astronaut is connected.")]
        Button m_StartButton;

        MissionFlow m_Flow;
        ScenarioManager m_Scenarios;
        GameStateSync m_Sync;
        bool m_Refreshing;

        void Start()
        {
            m_Flow = MissionFlow.Instance;
            m_Scenarios = ScenarioManager.Instance;
            m_Sync = GameStateSync.Active;
            if (m_Flow == null || m_Scenarios == null || m_Sync == null)
            {
                Debug.LogWarning("OperatorLobbyView needs a running operator session.", this);
                return;
            }

            var options = new List<string>();
            foreach (var satellite in m_Scenarios.Satellites)
                options.Add(satellite != null ? satellite.DisplayName : "-");

            m_SatelliteDropdown.ClearOptions();
            m_SatelliteDropdown.AddOptions(options);

            var addresses = LanDiscovery.GetLocalIPv4Addresses();
            m_AddressText.text = addresses.Count > 0 ? $"IP: {string.Join("  |  ", addresses)}" : "IP: -";

            m_SatelliteDropdown.onValueChanged.AddListener(OnSatelliteChanged);
            m_TutorialToggle.onValueChanged.AddListener(OnTutorialChanged);
            m_FaultCountSlider.onValueChanged.AddListener(OnFaultCountChanged);
            m_StartButton.onClick.AddListener(m_Flow.StartMission);

            m_Sync.StateReceived += Refresh;
            if (m_Sync.LatestState != null)
                Refresh(m_Sync.LatestState);
        }

        void OnDestroy()
        {
            if (m_Sync != null)
                m_Sync.StateReceived -= Refresh;
        }

        void Refresh(GameState state)
        {
            // Setting the controls from the state must not write the state back.
            m_Refreshing = true;

            m_StatusText.text = state.astronautConnected
                ? "Astronaut verbunden – bereit für den Missionsstart."
                : "Warte auf den Astronauten (Quest im selben Netzwerk starten) …";

            var satellites = m_Scenarios.Satellites;
            for (var i = 0; i < satellites.Count; i++)
            {
                if (satellites[i] != null && satellites[i].Id == state.settings.satelliteId)
                    m_SatelliteDropdown.SetValueWithoutNotify(i);
            }

            m_TutorialToggle.SetIsOnWithoutNotify(state.settings.tutorial);
            m_FaultCountSlider.SetValueWithoutNotify(state.settings.faultCount);
            m_FaultCountText.text = $"Fehler: {state.settings.faultCount}";
            m_StartButton.interactable = m_Flow.CanStartMission;

            m_Refreshing = false;
        }

        void OnSatelliteChanged(int index)
        {
            if (!m_Refreshing)
                m_Flow.SelectSatellite(index);
        }

        void OnTutorialChanged(bool enabled)
        {
            if (!m_Refreshing)
                m_Flow.SetTutorial(enabled);
        }

        void OnFaultCountChanged(float value)
        {
            if (!m_Refreshing)
                m_Flow.SetFaultCount(Mathf.RoundToInt(value));
        }
    }
}
