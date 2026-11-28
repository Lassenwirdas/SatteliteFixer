using SatelliteFixer.Core;
using SatelliteFixer.Network;
using UnityEngine;
using UnityEngine.Events;

namespace SatelliteFixer.Astronaut
{
    /// <summary>
    /// Put this on a sensor of a satellite model in the VR scene (AP 4.3). It tracks whether the sensor
    /// is faulty according to the game state and sends repair actions to the host.
    /// Wire <see cref="PerformRepair()"/> to an XRI interactable event, e.g. Select Entered of an XR Simple Interactable.
    /// </summary>
    public class RepairableSensor : MonoBehaviour
    {
        [SerializeField, Tooltip("Which sensor this object represents.")]
        SensorDefinition m_Sensor;

        [SerializeField, Tooltip("Action sent when PerformRepair() is called without an argument.")]
        RepairAction m_RepairAction = RepairAction.Reconnect;

        [SerializeField, Tooltip("Sync to talk to the host. Set automatically when spawned by the SatelliteSpawner.")]
        GameStateSync m_Sync;

        [SerializeField, Tooltip("Invoked when the sensor becomes faulty, e.g. to show damage or sparks.")]
        UnityEvent m_OnFaulty = new UnityEvent();

        [SerializeField, Tooltip("Invoked when the sensor has been repaired.")]
        UnityEvent m_OnRepaired = new UnityEvent();

        bool m_Subscribed;

        public SensorDefinition Sensor => m_Sensor;
        public bool IsFaulty { get; private set; }

        /// <summary>
        /// Connects this sensor to a sync at runtime. Called by the <see cref="SatelliteSpawner"/>.
        /// </summary>
        public void Initialize(GameStateSync sync)
        {
            Unsubscribe();
            m_Sync = sync;
            if (isActiveAndEnabled)
                Subscribe();
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Sends the configured repair action to the host, which checks it against the active faults.
        /// </summary>
        public void PerformRepair()
        {
            PerformRepair(m_RepairAction);
        }

        public void PerformRepair(RepairAction action)
        {
            if (m_Sync == null || m_Sensor == null)
            {
                Debug.LogWarning("RepairableSensor needs a sync and a sensor definition.", this);
                return;
            }

            m_Sync.SendRepairAttempt(new RepairAttempt { sensorId = m_Sensor.Id, action = action });
        }

        void Subscribe()
        {
            if (m_Subscribed || m_Sync == null)
                return;

            m_Sync.StateReceived += OnStateReceived;
            m_Subscribed = true;

            if (m_Sync.LatestState != null)
                OnStateReceived(m_Sync.LatestState);
        }

        void Unsubscribe()
        {
            if (!m_Subscribed)
                return;

            if (m_Sync != null)
                m_Sync.StateReceived -= OnStateReceived;

            m_Subscribed = false;
        }

        void OnStateReceived(GameState state)
        {
            if (m_Sensor == null)
                return;

            var faulty = state.faults.Exists(fault => fault.sensorId == m_Sensor.Id && fault.status == FaultStatus.Active);
            if (faulty == IsFaulty)
                return;

            IsFaulty = faulty;
            if (faulty)
                m_OnFaulty.Invoke();
            else
                m_OnRepaired.Invoke();
        }
    }
}
