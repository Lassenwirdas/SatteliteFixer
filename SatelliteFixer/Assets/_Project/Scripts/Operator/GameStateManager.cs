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

        public GameState State => m_State;
        public GameStateSync Sync => m_Sync;

        string SavePath => Path.Combine(Application.persistentDataPath, m_SaveFileName);

        void Start()
        {
            if (m_LoadOnStart)
                TryLoad();

            m_Sync.PublishState(m_State);
        }

        /// <summary>
        /// Applies a change to the game state, then saves and publishes it.
        /// </summary>
        public void Modify(Action<GameState> change)
        {
            change(m_State);
            m_State.revision++;
            Save();
            m_Sync.PublishState(m_State);
        }

        /// <summary>
        /// Returns to the lobby with no active scenario.
        /// </summary>
        public void ResetState()
        {
            Modify(state =>
            {
                state.phase = ScenarioPhase.Lobby;
                state.satelliteId = string.Empty;
                state.faults.Clear();
            });
        }

        public void Save()
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(m_State, true));
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
