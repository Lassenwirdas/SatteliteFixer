using System;
using System.IO;
using SatelliteFixer.Core;
using SatelliteFixer.Network;
using UnityEngine;

namespace SatelliteFixer.Operator
{
    /// <summary>
    /// Owns the authoritative <see cref="GameState"/> on the PC app (host, AP 5.2).
    /// Every change goes through <see cref="Modify"/>, which bumps the revision, saves the state
    /// locally and publishes it to the VR app.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Sync used to publish the state to the VR app.")]
        GameStateSync m_Sync;

        [SerializeField, Tooltip("File name of the local save in Application.persistentDataPath.")]
        string m_SaveFileName = "gamestate.json";

        [SerializeField, Tooltip("Load the last saved state on start instead of starting in the lobby.")]
        bool m_LoadOnStart;

        GameState m_State = new GameState();

        /// <summary>
        /// The game state owner of the running session, for scene objects that cannot reference it directly.
        /// </summary>
        public static GameStateManager Instance { get; private set; }

        public GameState State => m_State;
        public GameStateSync Sync => m_Sync;

        string SavePath => Path.Combine(Application.persistentDataPath, m_SaveFileName);

        void OnEnable()
        {
            Instance = this;
        }

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            if (m_LoadOnStart)
                TryLoad();

            m_Sync.PublishState(m_State);
        }

        /// <summary>
        /// Applies a change to the game state, then saves and publishes it.
        /// Pass <paramref name="save"/> = false for frequent updates such as the mission clock.
        /// </summary>
        public void Modify(Action<GameState> change, bool save = true)
        {
            change(m_State);
            m_State.revision++;
            if (save)
                Save();

            m_Sync.PublishState(m_State);
        }

        /// <summary>
        /// Returns to the lobby with no active mission. The lobby settings are kept.
        /// </summary>
        public void ResetState()
        {
            Modify(state =>
            {
                state.phase = ScenarioPhase.Lobby;
                state.satelliteId = string.Empty;
                state.faults.Clear();
                state.astronautReady = false;
                state.missionTime = 0f;
                state.paused = false;
            });
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(m_State, true));
            }
            catch (IOException e)
            {
                Debug.LogWarning($"Could not save game state: {e.Message}", this);
            }
        }

        public bool TryLoad()
        {
            if (!File.Exists(SavePath))
                return false;

            var loaded = JsonUtility.FromJson<GameState>(File.ReadAllText(SavePath));
            if (loaded == null)
                return false;

            m_State = loaded;
            return true;
        }
    }
}
