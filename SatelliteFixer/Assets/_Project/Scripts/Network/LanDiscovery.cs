using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace SatelliteFixer.Network
{
    /// <summary>
    /// Lets the VR app find the PC host in the local network: the host broadcasts its game port via UDP
    /// about once per second, the client listens and reports every host it hears.
    /// Plain sockets polled in Update, so everything runs on the main thread.
    /// </summary>
    public class LanDiscovery : MonoBehaviour
    {
        const string k_MessagePrefix = "SATFIX1";

        [SerializeField, Tooltip("UDP port used for discovery broadcasts (not the game port).")]
        int m_DiscoveryPort = 47777;

        [SerializeField, Tooltip("Seconds between two broadcasts of the host.")]
        float m_BroadcastInterval = 1f;

        UdpClient m_Socket;
        bool m_Advertising;
        ushort m_AdvertisedGamePort;
        float m_NextBroadcastTime;
        List<IPEndPoint> m_BroadcastTargets;
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject m_MulticastLock;
#endif

        /// <summary>
        /// Raised on the client for every received host announcement, with the host's address and game port.
        /// </summary>
        public event Action<string, ushort> HostFound;

        public bool IsAdvertising => m_Advertising;
        public bool IsListening => m_Socket != null && !m_Advertising;

        /// <summary>
        /// Host side: announces the given game port to the local network until <see cref="Stop"/> is called.
        /// </summary>
        public void StartAdvertising(ushort gamePort)
        {
            Stop();
            try
            {
                m_Socket = new UdpClient { EnableBroadcast = true };
                m_BroadcastTargets = GetBroadcastTargets();
                m_AdvertisedGamePort = gamePort;
                m_Advertising = true;
                m_NextBroadcastTime = 0f;
            }
            catch (SocketException e)
            {
                Debug.LogWarning($"[Discovery] Could not start advertising: {e.Message}");
                Stop();
            }
        }

        /// <summary>
        /// Client side: listens for host announcements until <see cref="Stop"/> is called.
        /// </summary>
        public void StartListening()
        {
            Stop();
            try
            {
                m_Socket = new UdpClient();
                m_Socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                m_Socket.Client.Bind(new IPEndPoint(IPAddress.Any, m_DiscoveryPort));
                AcquireMulticastLock();
            }
            catch (SocketException e)
            {
                Debug.LogWarning($"[Discovery] Could not listen on port {m_DiscoveryPort}: {e.Message}");
                Stop();
            }
        }

        public void Stop()
        {
            m_Advertising = false;
            m_Socket?.Close();
            m_Socket = null;
            ReleaseMulticastLock();
        }

        void OnDestroy()
        {
            Stop();
        }

        void Update()
        {
            if (m_Socket == null)
                return;

            try
            {
                if (m_Advertising)
                    Broadcast();
                else
                    Receive();
            }
            catch (SocketException e)
            {
                Debug.LogWarning($"[Discovery] {e.Message}");
            }
        }

        void Broadcast()
        {
            if (Time.unscaledTime < m_NextBroadcastTime)
                return;

            m_NextBroadcastTime = Time.unscaledTime + m_BroadcastInterval;
            var payload = Encoding.UTF8.GetBytes($"{k_MessagePrefix}|{m_AdvertisedGamePort}|{SystemInfo.deviceName}");
            foreach (var target in m_BroadcastTargets)
            {
                try
                {
                    m_Socket.Send(payload, payload.Length, target);
                }
                catch (SocketException)
                {
                    // An interface may be down or refuse broadcasts; the other targets still work.
                }
            }
        }

        void Receive()
        {
            while (m_Socket.Available > 0)
            {
                var sender = new IPEndPoint(IPAddress.Any, 0);
                var data = m_Socket.Receive(ref sender);
                var parts = Encoding.UTF8.GetString(data).Split('|');
                if (parts.Length >= 2 && parts[0] == k_MessagePrefix && ushort.TryParse(parts[1], out var gamePort))
                    HostFound?.Invoke(sender.Address.ToString(), gamePort);
            }
        }

        List<IPEndPoint> GetBroadcastTargets()
        {
            // The limited broadcast only leaves through one adapter on Windows, so also send to the
            // directed broadcast address of every IPv4 network, plus loopback for same-machine tests.
            var targets = new List<IPEndPoint>
            {
                new IPEndPoint(IPAddress.Broadcast, m_DiscoveryPort),
                new IPEndPoint(IPAddress.Loopback, m_DiscoveryPort),
            };

            foreach (var (address, mask) in GetLocalIPv4Networks())
            {
                var addressBytes = address.GetAddressBytes();
                var maskBytes = mask.GetAddressBytes();
                var broadcast = new byte[4];
                for (var i = 0; i < 4; i++)
                    broadcast[i] = (byte)(addressBytes[i] | ~maskBytes[i]);

                targets.Add(new IPEndPoint(new IPAddress(broadcast), m_DiscoveryPort));
            }

            return targets;
        }

        /// <summary>
        /// IPv4 addresses of this device that are up and not loopback, e.g. to show the host's address in the lobby.
        /// </summary>
        public static List<string> GetLocalIPv4Addresses()
        {
            var addresses = new List<string>();
            foreach (var (address, _) in GetLocalIPv4Networks())
                addresses.Add(address.ToString());

            return addresses;
        }

        static IEnumerable<(IPAddress address, IPAddress mask)> GetLocalIPv4Networks()
        {
            NetworkInterface[] interfaces;
            try
            {
                interfaces = NetworkInterface.GetAllNetworkInterfaces();
            }
            catch (NetworkInformationException)
            {
                yield break;
            }

            foreach (var networkInterface in interfaces)
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up
                    || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                foreach (var unicast in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork && unicast.IPv4Mask != null)
                        yield return (unicast.Address, unicast.IPv4Mask);
                }
            }
        }

        void AcquireMulticastLock()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Some Android devices drop incoming broadcasts unless a multicast lock is held.
            // Needs the CHANGE_WIFI_MULTICAST_STATE permission; without it we just try without the lock.
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var wifi = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
                m_MulticastLock = wifi.Call<AndroidJavaObject>("createMulticastLock", "SatelliteFixerDiscovery");
                m_MulticastLock.Call("acquire");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Discovery] No multicast lock, broadcasts may be filtered: {e.Message}");
                m_MulticastLock = null;
            }
#endif
        }

        void ReleaseMulticastLock()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (m_MulticastLock == null)
                return;

            m_MulticastLock.Call("release");
            m_MulticastLock.Dispose();
            m_MulticastLock = null;
#endif
        }
    }
}
