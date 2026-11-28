using SatelliteFixer.Core;
using UnityEngine;

namespace SatelliteFixer.Operator
{
    /// <summary>
    /// Drives the session flow on the host: Lobby → Loading → Mission → Debrief → Lobby.
    /// The phase lives in the game state, and both apps load their own scene from it.
    /// </summary>
    public class MissionFlow : MonoBehaviour
    {
        [SerializeField, Tooltip("Game state owner of the session.")]
        GameStateManager m_GameStateManager;

        [SerializeField, Tooltip("Rolls the faults when the mission begins.")]
        ScenarioManager m_ScenarioManager;

        [SerializeField, Min(1f), Tooltip("Mission ends with a debrief after this many seconds (decision: max. 15 minutes).")]
        float m_MaxMissionSeconds = 15f * 60f;

        [SerializeField, Min(0.1f), Tooltip("How often the mission clock is sent to the VR app, in seconds.")]
        float m_ClockPublishInterval = 0.5f;

        [SerializeField, Range(1, 5), Tooltip("Fault count preset in the lobby.")]
        int m_DefaultFaultCount = 2;

        float m_NextClockPublish;

        /// <summary>
        /// The mission flow of the running session, for scene objects that cannot reference it directly.
        /// </summary>
        public static MissionFlow Instance { get; private set; }

        GameState State => m_GameStateManager.State;

        /// <summary>
        /// True if the operator may start the mission: in the lobby, satellite chosen and astronaut connected.
        /// </summary>
        public bool CanStartMission => State.phase == ScenarioPhase.Lobby
            && State.astronautConnected
            && m_ScenarioManager.FindSatellite(State.settings.satelliteId) != null;

        void OnEnable()
        {
            Instance = this;
            m_GameStateManager.Sync.ClientReadyReceived += OnClientReady;
            m_GameStateManager.Sync.PeerConnectionChanged += OnPeerConnectionChanged;
        }

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;

            m_GameStateManager.Sync.ClientReadyReceived -= OnClientReady;
            m_GameStateManager.Sync.PeerConnectionChanged -= OnPeerConnectionChanged;
        }

        void Start()
        {
            // Preselect the first satellite so the operator can start right away.
            var satellites = m_ScenarioManager.Satellites;
            m_GameStateManager.Modify(state =>
            {
                state.settings.faultCount = m_DefaultFaultCount;
                if (string.IsNullOrEmpty(state.settings.satelliteId) && satellites.Count > 0 && satellites[0] != null)
                    state.settings.satelliteId = satellites[0].Id;
            });
        }

        void Update()
        {
            if (State.phase != ScenarioPhase.Mission || State.paused)
                return;

            State.missionTime += Time.deltaTime;
            if (State.missionTime >= m_MaxMissionSeconds)
            {
                EndMission();
                return;
            }

            if (Time.unscaledTime >= m_NextClockPublish)
            {
                m_NextClockPublish = Time.unscaledTime + m_ClockPublishInterval;
                m_GameStateManager.Modify(_ => { }, save: false);
            }
        }

        public void SelectSatellite(int index)
        {
            var satellites = m_ScenarioManager.Satellites;
            if (State.phase != ScenarioPhase.Lobby || index < 0 || index >= satellites.Count || satellites[index] == null)
                return;

            m_GameStateManager.Modify(state => state.settings.satelliteId = satellites[index].Id);
        }

        public void SetTutorial(bool enabled)
        {
            if (State.phase == ScenarioPhase.Lobby)
                m_GameStateManager.Modify(state => state.settings.tutorial = enabled);
        }

        public void SetFaultCount(int count)
        {
            if (State.phase == ScenarioPhase.Lobby)
                m_GameStateManager.Modify(state => state.settings.faultCount = Mathf.Max(1, count));
        }

        /// <summary>
        /// Lobby → Loading. Both apps load their role scene; the mission begins once the astronaut is ready.
        /// </summary>
        public void StartMission()
        {
            if (!CanStartMission)
            {
                Debug.LogWarning("Cannot start the mission: astronaut not connected or no satellite selected.", this);
                return;
            }

            m_GameStateManager.Modify(state =>
            {
                state.phase = ScenarioPhase.Loading;
                state.satelliteId = state.settings.satelliteId;
                state.astronautReady = false;
                state.faults.Clear();
                state.missionTime = 0f;
                state.paused = false;
            });
        }

        /// <summary>
        /// Mission → Debrief, e.g. when time is up or the operator aborts.
        /// </summary>
        public void EndMission()
        {
            if (State.phase == ScenarioPhase.Mission || State.phase == ScenarioPhase.Loading)
                m_GameStateManager.Modify(state => state.phase = ScenarioPhase.Debrief);
        }

        /// <summary>
        /// Debrief (or any phase) → Lobby.
        /// </summary>
        public void ReturnToLobby()
        {
            m_GameStateManager.ResetState();
        }

        void OnClientReady()
        {
            if (State.phase != ScenarioPhase.Loading)
                return;

            m_GameStateManager.Modify(state => state.astronautReady = true);
            m_ScenarioManager.BeginMission(State.satelliteId, State.settings.faultCount);
        }

        void OnPeerConnectionChanged(bool connected)
        {
            m_GameStateManager.Modify(state =>
            {
                state.astronautConnected = connected;
                if (!connected)
                    state.astronautReady = false;

                // The clock stops while the astronaut is gone and resumes after the reconnect.
                if (state.phase == ScenarioPhase.Mission)
                    state.paused = !connected;
            });
        }
    }
}
