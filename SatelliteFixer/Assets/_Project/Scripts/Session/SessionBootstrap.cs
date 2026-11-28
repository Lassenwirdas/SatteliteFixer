using UnityEngine;

namespace SatelliteFixer.Session
{
    /// <summary>
    /// Put one into every scene: creates the session prefab if no session exists yet.
    /// That way every scene can be played directly in the editor, and the session survives scene changes.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class SessionBootstrap : MonoBehaviour
    {
        [SerializeField, Tooltip("Session prefab with NetworkManager, sync, discovery and the host components.")]
        GameObject m_SessionPrefab;

        void Awake()
        {
            if (Session.Instance == null && m_SessionPrefab != null)
                Instantiate(m_SessionPrefab).name = m_SessionPrefab.name;
        }
    }
}
