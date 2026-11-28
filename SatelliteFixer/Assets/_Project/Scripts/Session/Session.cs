using SatelliteFixer.Network;
using UnityEngine;
using UnityEngine.XR.Management;

namespace SatelliteFixer.Session
{
    /// <summary>
    /// Root of the session prefab, which lives in DontDestroyOnLoad across all scenes.
    /// Resolves the role of this app and starts the network accordingly:
    /// the operator hosts and advertises itself, the astronaut searches for the host and connects.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class Session : MonoBehaviour
    {
        /// <summary>Command line argument to skip discovery and connect to a fixed host, e.g. <c>-sfHost 192.168.0.10</c>.</summary>
        public const string HostArgument = "-sfHost";

        const float k_SwitchAddressAfterSeconds = 6f;

        [SerializeField, Tooltip("Editor only: force a role instead of using the platform / Multiplayer Play Mode.")]
        RoleOverride m_EditorRole = RoleOverride.Automatic;

        [SerializeField, Tooltip("Network transport of the session.")]
        NetcodeGameStateSync m_Sync;

        [SerializeField, Tooltip("Finds the host in the local network.")]
        LanDiscovery m_Discovery;

        [SerializeField, Tooltip("Components that only run on the host (game state, scenarios, mission flow).")]
        Behaviour[] m_HostOnlyComponents = new Behaviour[0];

        [SerializeField, Tooltip("Astronaut only: connect to this address instead of using discovery. Leave empty for discovery.")]
        string m_FixedHostAddress = string.Empty;

        float m_ConnectStartTime;

        public static Session Instance { get; private set; }

        public AppRole Role { get; private set; }
        public NetcodeGameStateSync Sync => m_Sync;
        public LanDiscovery Discovery => m_Discovery;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Role = RoleResolver.Resolve(m_EditorRole);
            Debug.Log($"[Session] Role: {Role}");

            if (Role == AppRole.Astronaut)
            {
                foreach (var component in m_HostOnlyComponents)
                {
                    if (component != null)
                        component.enabled = false;
                }
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            if (Role == AppRole.Operator)
            {
                StopXR();
                if (m_Sync.StartHost())
                    m_Discovery.StartAdvertising(m_Sync.Port);
            }
            else
            {
                var fixedAddress = CommandLine.GetValue(HostArgument);
                if (string.IsNullOrEmpty(fixedAddress))
                    fixedAddress = m_FixedHostAddress;

                if (!string.IsNullOrEmpty(fixedAddress))
                {
                    ConnectTo(fixedAddress);
                }
                else
                {
                    m_Discovery.HostFound += OnHostFound;
                    m_Discovery.StartListening();
                }
            }
        }

        /// <summary>
        /// Astronaut: connects to the host at the given address, e.g. from a manual IP entry.
        /// </summary>
        public void ConnectTo(string address)
        {
            Debug.Log($"[Session] Connecting to host {address}.");
            m_ConnectStartTime = Time.unscaledTime;
            m_Sync.StartClient(address);
        }

        void OnHostFound(string address, ushort port)
        {
            if (m_Sync.IsPeerConnected || m_Sync.HostAddress == address)
                return;

            // The host is heard on several addresses (LAN and loopback). Stick to the first one
            // unless it has not connected for a while, then try the next announcement.
            if (m_Sync.IsConnecting && Time.unscaledTime - m_ConnectStartTime < k_SwitchAddressAfterSeconds)
                return;

            if (port != m_Sync.Port)
                Debug.LogWarning($"[Session] Host {address} announces port {port}, but this app uses {m_Sync.Port}.");

            ConnectTo(address);
        }

        static void StopXR()
        {
            // The operator does not need VR. XR may still start on Windows (e.g. for Quest Link in the editor).
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager == null || !manager.isInitializationComplete)
                return;

            manager.StopSubsystems();
            manager.DeinitializeLoader();
        }
    }
}
