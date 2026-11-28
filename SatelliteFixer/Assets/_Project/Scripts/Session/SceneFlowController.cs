using SatelliteFixer.Core;
using SatelliteFixer.Network;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SatelliteFixer.Session
{
    /// <summary>
    /// Loads the scene that matches the current phase and this app's role (decision 8):
    /// Lobby → "Lobby", Loading/Mission/Debrief → "Operator" or "Astronaut".
    /// On the astronaut it also reports to the host once the mission scene is loaded.
    /// </summary>
    public class SceneFlowController : MonoBehaviour
    {
        public const string LobbyScene = "Lobby";
        public const string OperatorScene = "Operator";
        public const string AstronautScene = "Astronaut";

        [SerializeField, Tooltip("Session that knows the role and the sync.")]
        Session m_Session;

        string m_LoadingScene;
        bool m_ReadySent;

        GameStateSync Sync => m_Session.Sync;

        void OnEnable()
        {
            Sync.StateReceived += OnStateReceived;
            Sync.PeerConnectionChanged += OnPeerConnectionChanged;
        }

        void OnDisable()
        {
            Sync.StateReceived -= OnStateReceived;
            Sync.PeerConnectionChanged -= OnPeerConnectionChanged;
        }

        public string RoleScene => m_Session.Role == AppRole.Operator ? OperatorScene : AstronautScene;

        string TargetScene(ScenarioPhase phase) => phase == ScenarioPhase.Lobby ? LobbyScene : RoleScene;

        void OnStateReceived(GameState state)
        {
            if (state.phase == ScenarioPhase.Lobby)
                m_ReadySent = false;

            // While a scene loads, the latest state is checked again once it is done.
            if (m_LoadingScene != null)
                return;

            var target = TargetScene(state.phase);
            if (SceneManager.GetActiveScene().name != target)
            {
                Load(target);
                return;
            }

            TrySendReady(state);
        }

        void OnPeerConnectionChanged(bool connected)
        {
            // After a reconnect during Loading the host needs to hear "ready" again.
            if (!connected)
                m_ReadySent = false;
        }

        void Load(string scene)
        {
            Debug.Log($"[SceneFlow] Loading scene {scene}.");
            m_LoadingScene = scene;
            var operation = SceneManager.LoadSceneAsync(scene);
            if (operation == null)
            {
                Debug.LogError($"[SceneFlow] Scene '{scene}' is not in the build profile.");
                m_LoadingScene = null;
                return;
            }

            operation.completed += _ =>
            {
                m_LoadingScene = null;
                if (Sync.LatestState != null)
                    OnStateReceived(Sync.LatestState);
            };
        }

        void TrySendReady(GameState state)
        {
            if (m_Session.Role != AppRole.Astronaut || m_ReadySent || state.phase != ScenarioPhase.Loading)
                return;

            Debug.Log("[SceneFlow] Mission scene loaded, reporting ready.");
            m_ReadySent = true;
            Sync.SendClientReady();
        }
    }
}
