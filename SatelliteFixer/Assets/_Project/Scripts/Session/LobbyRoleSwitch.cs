using UnityEngine;

namespace SatelliteFixer.Session
{
    /// <summary>
    /// Activates the objects of this app's role in a shared scene, e.g. the PC canvas or the XR rig in the lobby.
    /// Keep both groups inactive in the scene; this switches the right one on.
    /// </summary>
    public class LobbyRoleSwitch : MonoBehaviour
    {
        [SerializeField, Tooltip("Activated on the PC app (operator).")]
        GameObject[] m_OperatorObjects = new GameObject[0];

        [SerializeField, Tooltip("Activated on the VR app (astronaut).")]
        GameObject[] m_AstronautObjects = new GameObject[0];

        void Awake()
        {
            var isOperator = Session.Instance == null || Session.Instance.Role == AppRole.Operator;
            foreach (var target in m_OperatorObjects)
            {
                if (target != null)
                    target.SetActive(isOperator);
            }

            foreach (var target in m_AstronautObjects)
            {
                if (target != null)
                    target.SetActive(!isOperator);
            }
        }
    }
}
