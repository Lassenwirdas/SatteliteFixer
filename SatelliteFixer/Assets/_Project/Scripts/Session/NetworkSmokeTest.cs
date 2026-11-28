using SatelliteFixer.Core;
using SatelliteFixer.Operator;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SatelliteFixer.Session
{
    /// <summary>
    /// Automated end-to-end check of network and scene flow, only active with <c>-sfSmokeTest</c> on the command line.
    /// Operator: waits for the astronaut, starts the mission, ends it, returns to the lobby.
    /// Astronaut: follows along. Both log "[SmokeTest] PASS" or "FAIL" and quit (exit code 0 or 1).
    /// </summary>
    public class NetworkSmokeTest : MonoBehaviour
    {
        public const string Flag = "-sfSmokeTest";
        public const string MissionSecondsArgument = "-sfSmokeMissionSeconds";

        [SerializeField, Tooltip("Session under test.")]
        Session m_Session;

        [SerializeField, Tooltip("Fails the test if it does not finish within this many seconds.")]
        float m_Timeout = 120f;

        [SerializeField, Tooltip("How long the operator keeps the mission running before ending it.")]
        float m_MissionSeconds = 3f;

        bool m_Active;
        bool m_SawMission;
        bool m_SawDebrief;
        bool m_Finished;
        bool m_LastPaused;
        float m_PhaseSince;
        ScenarioPhase m_LastPhase = ScenarioPhase.Lobby;

        void Start()
        {
            m_Active = CommandLine.HasFlag(Flag);
            if (!m_Active)
                return;

            // A longer mission leaves time to kill and restart the astronaut to test the reconnect.
            if (float.TryParse(CommandLine.GetValue(MissionSecondsArgument), out var missionSeconds))
            {
                m_MissionSeconds = missionSeconds;
                m_Timeout = Mathf.Max(m_Timeout, missionSeconds + 60f);
            }

            Debug.Log($"[SmokeTest] Running as {m_Session.Role}.");
        }

        void Update()
        {
            if (!m_Active || m_Finished)
                return;

            if (Time.realtimeSinceStartup > m_Timeout)
            {
                Finish(false, $"timeout in phase {m_LastPhase}, scene {SceneManager.GetActiveScene().name}");
                return;
            }

            var state = m_Session.Sync.LatestState;
            if (state == null)
                return;

            if (state.paused != m_LastPaused)
            {
                Debug.Log($"[SmokeTest] Clock {(state.paused ? "paused" : "running")} at {state.missionTime:0.0}s, astronaut connected: {state.astronautConnected}");
                m_LastPaused = state.paused;
            }

            if (state.phase != m_LastPhase)
            {
                Debug.Log($"[SmokeTest] Phase {m_LastPhase} -> {state.phase} (scene {SceneManager.GetActiveScene().name})");
                m_LastPhase = state.phase;
                m_PhaseSince = Time.realtimeSinceStartup;
                m_SawMission |= state.phase == ScenarioPhase.Mission;
                m_SawDebrief |= state.phase == ScenarioPhase.Debrief;
            }

            var inPhaseFor = Time.realtimeSinceStartup - m_PhaseSince;
            var sceneMatches = SceneManager.GetActiveScene().name ==
                (state.phase == ScenarioPhase.Lobby ? SceneFlowController.LobbyScene :
                    m_Session.Role == AppRole.Operator ? SceneFlowController.OperatorScene : SceneFlowController.AstronautScene);

            if (m_Session.Role == AppRole.Operator)
                DriveOperator(state, inPhaseFor, sceneMatches);
            else if (m_SawMission && m_SawDebrief && state.phase == ScenarioPhase.Lobby && sceneMatches)
                Finish(true, "followed the full flow");
        }

        void DriveOperator(GameState state, float inPhaseFor, bool sceneMatches)
        {
            var flow = MissionFlow.Instance;
            if (flow == null)
                return;

            switch (state.phase)
            {
                case ScenarioPhase.Lobby when !m_SawMission && flow.CanStartMission && inPhaseFor > 1f:
                    flow.StartMission();
                    break;
                case ScenarioPhase.Mission when sceneMatches && inPhaseFor > m_MissionSeconds:
                    flow.EndMission();
                    break;
                case ScenarioPhase.Debrief when sceneMatches && inPhaseFor > 1f:
                    flow.ReturnToLobby();
                    break;
                // Give the astronaut a moment to load the lobby too before the host goes away.
                case ScenarioPhase.Lobby when m_SawDebrief && sceneMatches && inPhaseFor > 5f:
                    Finish(true, "drove the full flow");
                    break;
            }
        }

        void Finish(bool passed, string reason)
        {
            m_Finished = true;
            Debug.Log($"[SmokeTest] {(passed ? "PASS" : "FAIL")}: {reason}");
            Application.Quit(passed ? 0 : 1);
        }
    }
}
