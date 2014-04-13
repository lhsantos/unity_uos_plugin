using System.Collections.Generic;
using System.Threading;


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
    /// Base class for all network radar implementations.
    /// </summary>
    public abstract class NetworkRadar : Radar
    {
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
        protected Logger logger;


        protected NetworkRadar(Logger logger = null)
        {
            running = false;
            this.logger = logger;
        }

        /// <summary>
        /// Starts this radar.
        /// </summary>
        public virtual void StartRadar()
        {
            if (running)
                logger.LogWarning("Radar on already running.");
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
            if (DevicesChanged != null)
                DevicesChanged(this, RadarEvent.DEVICE_ENTERED, device);
        }

        /// <summary>
        /// Notifies listeners that a device left this Radar's network.
        /// </summary>
        /// <param name="device">The device.</param>
        protected void RaiseDeviceLeft(NetworkDevice device)
        {
            if (DevicesChanged != null)
                DevicesChanged(this, RadarEvent.DEVICE_LEFT, device);
        }
    }
}
