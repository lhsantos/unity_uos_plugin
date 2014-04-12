using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


namespace UOS
{
    /// <summary>
    /// What are the possible types of connections over ethernet?
    /// </summary>
    public enum EthernetConnectionType
    {
        TCP,
        UDP,
        RTP
    }

    /// <summary>
    /// A generic device found on a network.
    /// </summary>
    public class NetworkDevice
    {
        private const string NETWORK_DEVICE_TYPE = "Ethernet";

        public string host { get; private set; }
        public int port { get; private set; }
        public EthernetConnectionType connectionType { get; private set; }

        public string networkDeviceName { get { return host + ":" + port; } }
        public string networkDeviceType { get { return NETWORK_DEVICE_TYPE + ":" + connectionType.ToString(); } }

        public NetworkDevice(string host, int port, EthernetConnectionType connectionType)
        {
            this.host = host;
            this.port = port;
            this.connectionType = connectionType;
        }
    }

    /// <summary>
    /// Possible radar events to be fired by Unity radars.
    /// </summary>
    public enum RadarEvent
    {
        DEVICE_ENTERED,
        DEVICE_LEFT
    }

    /// <summary>
    /// Base class for all Unity radar implementations.
    /// </summary>
    public abstract class UnityRadar : MonoBehaviour, Radar
    {
        private struct InternalEvent
        {
            public RadarEvent type;
            public NetworkDevice device;
        }


        /// <summary>
        /// UnityRadar events' handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="type"></param>
        /// <param name="device"></param>
        public delegate void RadarEventHandler(object sender, RadarEvent type, NetworkDevice device);

        /// <summary>
        /// Registers events for a radar.
        /// </summary>
        public event RadarEventHandler DevicesChanged;

        /// <summary>
        /// Is this radar running?
        /// </summary>
        protected bool running;

        private Queue<InternalEvent> events = new Queue<InternalEvent>();
        private readonly object _queue_lock = new object();


        /// <summary>
        /// Called when this object is created.
        /// </summary>
        protected virtual void Awake()
        {
            running = false;
        }

        /// <summary>
        /// If a subclass overrides this method and does not call this base implementation,
        /// wild rabbits will bite you to death!
        /// </summary>
        protected virtual void Update()
        {
            lock (_queue_lock)
            {
                while (events.Count > 0)
                {
                    InternalEvent e = events.Dequeue();
                    if (DevicesChanged != null)
                        DevicesChanged(this, e.type, e.device);
                }
            }
        }

        /// <summary>
        /// Starts this radar.
        /// </summary>
        public virtual void StartRadar()
        {
            if (running)
                Debug.LogWarning("Radar on " + name + " already running.");
            else
            {
                running = true;
                Thread t = new Thread(new ThreadStart(RadarThread));
                t.Start();
            }
        }

        /// <summary>
        /// Stops this radar.
        /// </summary>
        public virtual void StopRadar()
        {
            running = false;
        }

        /// <summary>
        /// Subclasses must implement this method, that will be the main thread of the Radar.
        /// </summary>
        protected abstract void RadarThread();

        /// <summary>
        /// Notifies listeners that a device entered this Radar's network.
        /// </summary>
        /// <param name="device">The device.</param>
        protected void RaiseDeviceEntered(NetworkDevice device)
        {
            lock (_queue_lock)
            {
                events.Enqueue(new InternalEvent { type = RadarEvent.DEVICE_ENTERED, device = device });
            }
        }

        /// <summary>
        /// Notifies listeners that a device left this Radar's network.
        /// </summary>
        /// <param name="device">The device.</param>
        protected void RaiseDeviceLeft(NetworkDevice device)
        {
            lock (_queue_lock)
            {
                events.Enqueue(new InternalEvent { type = RadarEvent.DEVICE_LEFT, device = device });
            }
        }
    }
}
