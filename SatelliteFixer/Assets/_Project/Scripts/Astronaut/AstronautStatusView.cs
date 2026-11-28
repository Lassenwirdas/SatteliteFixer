using SatelliteFixer.Core;
using SatelliteFixer.Network;
using TMPro;
using UnityEngine;

namespace SatelliteFixer.Astronaut
{
    /// <summary>
    /// World-space status text for the VR app, used in the lobby and in the mission scene:
    /// connection to mission control, waiting for the start, and the result in the debrief.
    /// </summary>
    public class AstronautStatusView : MonoBehaviour
    {
        [SerializeField, Tooltip("Text the status is written to.")]
        TMP_Text m_Text;

        [SerializeField, Tooltip("Hide the text while the mission is running, so it does not block the view.")]
        bool m_HideDuringMission = true;

        void Update()
        {
            var sync = GameStateSync.Active;
            var text = sync != null ? Describe(sync) : string.Empty;
            if (m_Text.text != text)
                m_Text.text = text;
        }

        string Describe(GameStateSync sync)
        {
            if (!sync.IsPeerConnected)
            {
                var netcode = sync as NetcodeGameStateSync;
                return netcode != null && !string.IsNullOrEmpty(netcode.HostAddress)
                    ? $"Verbinde mit Mission Control ({netcode.HostAddress}) …"
                    : "Suche Mission Control im Netzwerk …";
            }

            var state = sync.LatestState;
            if (state == null)
                return "Verbunden – warte auf Daten …";

            switch (state.phase)
            {
                case ScenarioPhase.Lobby:
                    return "Verbunden mit Mission Control.\nWarte auf den Missionsstart …";
                case ScenarioPhase.Loading:
                    return "Mission wird geladen …";
                case ScenarioPhase.Mission:
                    return m_HideDuringMission ? string.Empty : FormatTime(state.missionTime);
                case ScenarioPhase.Debrief:
                    return $"Mission beendet!\nZeit: {FormatTime(state.missionTime)}";
                default:
                    return string.Empty;
            }
        }

        static string FormatTime(float seconds)
        {
            var total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
