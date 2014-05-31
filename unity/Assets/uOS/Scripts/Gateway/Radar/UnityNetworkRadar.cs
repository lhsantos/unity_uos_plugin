using System.Collections.Generic;


namespace UOS
{
    /// <summary>
    /// Possible radar events to be fired by Unity radars.
    /// </summary>
    public enum RadarEvent
    {
        DEVICE_ENTERED,
        DEVICE_LEFT
    }

    /// <summary>
    /// Base class for all Unity network radar implementations.
    /// </summary>
    public abstract class UnityNetworkRadar : IRadar, IUnityUpdatable
    {
        /// <summary>
        /// UnityRadar events' handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="type"></param>
        /// <param name="device"></param>
        public delegate void RadarEventHandler(object sender, RadarEvent type, SocketDevice device);

        /// <summary>
        /// Registers events for a radar.
        /// </summary>
        public event RadarEventHandler DevicesChanged;


        /// <summary>
        /// The logger for this radar.
        /// </summary>
        protected Logger logger;

        private object _events_lock = new object();
        private Queue<object> eventQueue = new Queue<object>();

        /// <summary>
        /// Is this radar running?
        /// </summary>
        public bool running { get; protected set; }

        protected int QueueSize { get { return eventQueue.Count; } }


        protected UnityNetworkRadar(Logger logger = null)
        {
            this.running = false;
            this.logger = logger;
        }

        /// <summary>
        /// Starts this radar.
        /// </summary>
        public virtual void Init()
        {
            if (running)
                logger.LogWarning("Radar on already running.");
            else
                running = true;
        }

        /// <summary>
        /// Stops this radar.
        /// </summary>
        public virtual void TearDown()
        {
            running = false;
        }

        /// <summary>
        /// Updates this Unity radar.
        /// </summary>
        public virtual void Update()
        {
            if (running)
            {
                lock (_events_lock)
                {
                    while (eventQueue.Count > 0)
                        HandleEvent(eventQueue.Dequeue());
                }
            }
        }

        /// <summary>
        /// Enqueues an event to be dealt with in the next Unity iteration.
        /// </summary>
        /// <param name="evt"></param>
        protected void PushEvent(object evt)
        {
            lock (_events_lock)
            {
                eventQueue.Enqueue(evt);
            }
        }

        /// <summary>
        /// Removes an event from the top of the internal event queue.
        /// </summary>
        /// <returns></returns>
        protected object PopEvent()
        {
            lock (_events_lock)
            {
                return eventQueue.Dequeue();
            }
        }

        /// <summary>
        /// Allows subclasses to deal with a dequeued event on this iteration.
        /// </summary>
        /// <param name="evt"></param>
        protected abstract void HandleEvent(object evt);

        /// <summary>
        /// Notifies listeners that a device entered this Radar's network.
        /// </summary>
        /// <param name="device">The device.</param>
        protected void RaiseDeviceEntered(SocketDevice device)
        {
            if (DevicesChanged != null)
                DevicesChanged(this, RadarEvent.DEVICE_ENTERED, device);
        }

        /// <summary>
        /// Notifies listeners that a device left this Radar's network.
        /// </summary>
        /// <param name="device">The device.</param>
        protected void RaiseDeviceLeft(SocketDevice device)
        {
            if (DevicesChanged != null)
                DevicesChanged(this, RadarEvent.DEVICE_LEFT, device);
        }
    }
}
