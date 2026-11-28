using System.Collections.Generic;
using SatelliteFixer.Core;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace SatelliteFixer.Network
{
    /// <summary>
    /// <see cref="GameStateSync"/> over Netcode for GameObjects and Unity Transport in the local network (AP 3).
    /// Uses named messages instead of NetworkObjects, because the two apps load different scenes and
    /// Netcode's scene management is turned off. The game state travels as JSON.
    /// </summary>
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public class NetcodeGameStateSync : GameStateSync
    {
        const string k_StateMessage = "SatelliteFixer.State";
        const string k_RepairMessage = "SatelliteFixer.Repair";
        const string k_ReadyMessage = "SatelliteFixer.Ready";

        [SerializeField, Tooltip("UDP port the host listens on.")]
        ushort m_Port = 7777;

        [SerializeField, Tooltip("Seconds between connection attempts of the client while the host is not reachable.")]
        float m_ReconnectInterval = 2f;

        [SerializeField, Tooltip("A peer that sends nothing for this long counts as gone (Unity Transport's default is 30 s).")]
        int m_DisconnectTimeoutMs = 5000;

        NetworkManager m_NetworkManager;
        UnityTransport m_Transport;
        string m_HostAddress;
        bool m_WantClientConnection;
        float m_NextConnectTime;

        public ushort Port => m_Port;
        public override bool IsHost => m_NetworkManager != null && m_NetworkManager.IsServer;

        /// <summary>
        /// Address the client connects (or tries to connect) to, or null.
        /// </summary>
        public string HostAddress => m_HostAddress;

        /// <summary>
        /// True while the client is trying to reach the host but is not connected yet.
        /// </summary>
        public bool IsConnecting => m_WantClientConnection && !IsPeerConnected;

        void Awake()
        {
            m_NetworkManager = GetComponent<NetworkManager>();
            m_Transport = GetComponent<UnityTransport>();
            m_NetworkManager.NetworkConfig.NetworkTransport = m_Transport;

            // Notice a crashed Quest or lost Wi-Fi within seconds, so the mission clock pauses promptly.
            m_Transport.DisconnectTimeoutMS = m_DisconnectTimeoutMs;
            m_Transport.HeartbeatTimeoutMS = Mathf.Min(500, m_DisconnectTimeoutMs / 4);

            // Each app loads its own role scene, so Netcode must not synchronize scenes.
            m_NetworkManager.NetworkConfig.EnableSceneManagement = false;
            m_NetworkManager.NetworkConfig.ConnectionApproval = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_NetworkManager.OnServerStarted += OnServerStarted;
            m_NetworkManager.OnClientStarted += OnClientStarted;
            m_NetworkManager.OnClientConnectedCallback += OnClientConnected;
            m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            m_NetworkManager.OnClientStopped += OnClientStopped;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_NetworkManager.OnServerStarted -= OnServerStarted;
            m_NetworkManager.OnClientStarted -= OnClientStarted;
            m_NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            m_NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            m_NetworkManager.OnClientStopped -= OnClientStopped;
        }

        void Update()
        {
            // Keep trying until the host is reachable, and again after losing it (AP 3.5).
            if (!m_WantClientConnection || m_NetworkManager.IsListening || m_NetworkManager.ShutdownInProgress)
                return;

            if (Time.unscaledTime >= m_NextConnectTime)
                TryConnect();
        }

        /// <summary>
        /// Starts the PC app as host, listening on all network interfaces.
        /// </summary>
        public bool StartHost()
        {
            m_WantClientConnection = false;
            m_Transport.SetConnectionData("127.0.0.1", m_Port, "0.0.0.0");
            var started = m_NetworkManager.StartHost();
            Debug.Log($"[Network] Host {(started ? "started" : "failed to start")} on port {m_Port}.");
            return started;
        }

        /// <summary>
        /// Connects the VR app to the host at the given IPv4 address and keeps reconnecting if the connection drops.
        /// </summary>
        public void StartClient(string hostAddress)
        {
            if (m_WantClientConnection && hostAddress == m_HostAddress)
                return;

            if (m_NetworkManager.IsListening)
                m_NetworkManager.Shutdown();

            m_HostAddress = hostAddress;
            m_WantClientConnection = true;
            m_NextConnectTime = 0f;
        }

        /// <summary>
        /// Stops hosting or connecting.
        /// </summary>
        public void StopNetwork()
        {
            m_WantClientConnection = false;
            if (m_NetworkManager.IsListening)
                m_NetworkManager.Shutdown();
        }

        public override void PublishState(GameState state)
        {
            var json = JsonUtility.ToJson(state);
            OnStateReceived(JsonUtility.FromJson<GameState>(json));

            if (!m_NetworkManager.IsServer)
                return;

            var clients = RemoteClientIds();
            if (clients.Count > 0)
                Send(k_StateMessage, clients, json);
        }

        public override void SendRepairAttempt(RepairAttempt attempt)
        {
            if (m_NetworkManager.IsServer)
                OnRepairAttemptReceived(attempt);
            else if (IsPeerConnected)
                Send(k_RepairMessage, NetworkManager.ServerClientId, JsonUtility.ToJson(attempt));
        }

        public override void SendClientReady()
        {
            if (m_NetworkManager.IsServer)
                OnClientReadyReceived();
            else if (IsPeerConnected)
                Send(k_ReadyMessage, NetworkManager.ServerClientId, string.Empty);
        }

        void TryConnect()
        {
            m_NextConnectTime = Time.unscaledTime + m_ReconnectInterval;
            m_Transport.SetConnectionData(m_HostAddress, m_Port);
            if (!m_NetworkManager.StartClient())
                Debug.LogWarning($"[Network] Could not start client for {m_HostAddress}:{m_Port}.");
        }

        void OnServerStarted()
        {
            var messaging = m_NetworkManager.CustomMessagingManager;
            messaging.RegisterNamedMessageHandler(k_RepairMessage, OnRepairMessage);
            messaging.RegisterNamedMessageHandler(k_ReadyMessage, OnReadyMessage);
        }

        void OnClientStarted()
        {
            if (!m_NetworkManager.IsServer)
                m_NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(k_StateMessage, OnStateMessage);
        }

        void OnClientConnected(ulong clientId)
        {
            if (m_NetworkManager.IsServer)
            {
                if (clientId == NetworkManager.ServerClientId)
                    return;

                Debug.Log($"[Network] Astronaut connected (client {clientId}).");

                // There is only one astronaut. A new connection means the old one is stale (e.g. the app
                // crashed and restarted before the timeout), so drop it instead of waiting for the timeout.
                foreach (var otherId in RemoteClientIds())
                {
                    if (otherId != clientId)
                        m_NetworkManager.DisconnectClient(otherId, "Replaced by a new astronaut connection.");
                }

                OnPeerConnectionChanged(true);

                // Bring a new or reconnected client up to date right away.
                if (LatestState != null)
                    Send(k_StateMessage, new List<ulong> { clientId }, JsonUtility.ToJson(LatestState));
            }
            else if (clientId == m_NetworkManager.LocalClientId)
            {
                Debug.Log($"[Network] Connected to host {m_HostAddress}:{m_Port}.");
                OnPeerConnectionChanged(true);
            }
        }

        void OnClientDisconnected(ulong clientId)
        {
            if (m_NetworkManager.IsServer)
            {
                if (clientId == NetworkManager.ServerClientId || RemoteClientIds().Count > 0)
                    return;

                Debug.Log($"[Network] Astronaut disconnected (client {clientId}).");
                OnPeerConnectionChanged(false);
            }
        }

        void OnClientStopped(bool wasHost)
        {
            if (wasHost)
                return;

            if (IsPeerConnected)
                Debug.Log("[Network] Lost connection to host.");

            ResetLatestState();
            OnPeerConnectionChanged(false);
            m_NextConnectTime = Time.unscaledTime + m_ReconnectInterval;
        }

        void OnStateMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out string json);
            OnStateReceived(JsonUtility.FromJson<GameState>(json));
        }

        void OnRepairMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out string json);
            OnRepairAttemptReceived(JsonUtility.FromJson<RepairAttempt>(json));
        }

        void OnReadyMessage(ulong senderClientId, FastBufferReader reader)
        {
            OnClientReadyReceived();
        }

        List<ulong> RemoteClientIds()
        {
            var ids = new List<ulong>();
            foreach (var id in m_NetworkManager.ConnectedClientsIds)
            {
                if (id != NetworkManager.ServerClientId)
                    ids.Add(id);
            }

            return ids;
        }

        void Send(string messageName, ulong clientId, string payload)
        {
            Send(messageName, new List<ulong> { clientId }, payload);
        }

        void Send(string messageName, IReadOnlyList<ulong> clientIds, string payload)
        {
            using var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(payload), Allocator.Temp);
            writer.WriteValueSafe(payload);
            m_NetworkManager.CustomMessagingManager.SendNamedMessage(messageName, clientIds, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }
    }
}
