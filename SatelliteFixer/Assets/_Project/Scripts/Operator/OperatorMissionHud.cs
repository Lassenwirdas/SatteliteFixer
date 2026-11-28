using SatelliteFixer.Core;
using SatelliteFixer.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SatelliteFixer.Operator
{
    /// <summary>
    /// Mission frame of the PC app: clock, connection banner, end-mission button and the debrief panel.
    /// </summary>
    public class OperatorMissionHud : MonoBehaviour
    {
        [SerializeField, Tooltip("Mission clock (mm:ss).")]
        TMP_Text m_ClockText;

        [SerializeField, Tooltip("Shows loading and connection problems; empty while everything runs.")]
        TMP_Text m_BannerText;

        [SerializeField, Tooltip("Ends the mission early and goes to the debrief.")]
        Button m_EndMissionButton;

        [SerializeField, Tooltip("Shown in the debrief phase.")]
        GameObject m_DebriefPanel;

        [SerializeField, Tooltip("Result text in the debrief panel.")]
        TMP_Text m_DebriefText;

        [SerializeField, Tooltip("Returns both apps to the lobby.")]
        Button m_ReturnButton;

        MissionFlow m_Flow;
        GameStateSync m_Sync;

        void Start()
        {
            m_Flow = MissionFlow.Instance;
            m_Sync = GameStateSync.Active;
            if (m_Flow == null || m_Sync == null)
            {
                Debug.LogWarning("OperatorMissionHud needs a running operator session.", this);
                return;
            }

            m_EndMissionButton.onClick.AddListener(m_Flow.EndMission);
            m_ReturnButton.onClick.AddListener(m_Flow.ReturnToLobby);

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
            m_ClockText.text = FormatTime(state.missionTime);

            if (state.phase == ScenarioPhase.Loading)
                m_BannerText.text = "Astronaut lädt die Mission …";
            else if (!state.astronautConnected)
                m_BannerText.text = "Verbindung zum Astronauten verloren – Uhr angehalten.";
            else
                m_BannerText.text = string.Empty;

            m_EndMissionButton.gameObject.SetActive(state.phase == ScenarioPhase.Mission);

            var debrief = state.phase == ScenarioPhase.Debrief;
            m_DebriefPanel.SetActive(debrief);
            if (debrief)
            {
                var repaired = state.faults.FindAll(fault => fault.status == FaultStatus.Repaired).Count;
                m_DebriefText.text = $"Mission beendet\nZeit: {FormatTime(state.missionTime)}\nBehobene Fehler: {repaired} / {state.faults.Count}";
            }
        }

        public static string FormatTime(float seconds)
        {
            var total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
