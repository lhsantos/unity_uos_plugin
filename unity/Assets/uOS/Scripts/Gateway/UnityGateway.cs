﻿using MiniJSON;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;


namespace UOS
{
    /// <summary>
    /// This unity gateway keeps a register of connections and uP devices in the smart space.
    /// </summary>
    public class UnityGateway : IGateway, IUnityUpdatable
    {
        private struct ServiceCallEvent
        {
            public uOSServiceCallBack callback;
            public uOSServiceCallInfo info;
            public Response response;
            public System.Exception exception;
        }

        private struct ListenerInfo
        {
            public UOSEventListener listener;
            public UpDevice device;
            public string driver;
            public string instanceId;
            public string eventKey;
        }

        private const string DEVICE_DRIVER_NAME = "uos.DeviceDriver";
        private const string DRIVERS_NAME_KEY = "driversName";
        private const string INTERFACES_KEY = "interfaces";
        private const string REGISTER_EVENT_LISTENER_EVENT_KEY_PARAMETER = "eventKey";
        private const string REGISTER_LISTENER_SERVICE = "registerListener";
        private const string UNREGISTER_LISTENER_SERVICE = "unregisterListener";


        private uOSSettings settings;
        private IDictionary<string, ChannelManager> channelManagers = null;
        private GatewayServer server = null;
        private IDictionary<string, List<ListenerInfo>> listenerMap = new Dictionary<string, List<ListenerInfo>>();
        private Queue<ServiceCallEvent> serviceCallQueue = new Queue<ServiceCallEvent>();
        private object _service_call_queue_lock = new object();
        private UnityNetworkRadar radar = null;


        public Logger logger { get; private set; }
        public DriverManager driverManager { get; private set; }
        public DeviceManager deviceManager { get; private set; }


        /// <summary>
        /// Creates a new Gateway with given settings.
        /// </summary>
        /// <param name="uOSSettings"></param>
        public UnityGateway(uOSSettings uOSSettings, Logger logger)
        {
            this.settings = uOSSettings;
            this.logger = logger;

            PrepareChannels();
            PrepareDeviceAndDrivers();
            PrepareServer();
            PrepareRadar();
        }

        /// <summary>
        /// Initialises this Gateway.
        /// </summary>
        public void Init()
        {
            server.Init();

            if (radar != null)
                radar.StartRadar();
        }

        /// <summary>
        /// Releases this Gateway.
        /// </summary>
        public void TearDown()
        {
            if (radar != null)
            {
                radar.StopRadar();
                radar = null;
            }

            server.TearDown();
            server = null;

            foreach (ChannelManager cm in channelManagers.Values)
                cm.TearDown();
            channelManagers.Clear();
            channelManagers = null;
        }

        /// <summary>
        /// IUnityUpdatable update method, that will be called by uOS to process events
        /// inside Unity's thread.
        /// </summary>
        public void Update()
        {
            // Processes async events...
            lock (_service_call_queue_lock)
            {
                while (serviceCallQueue.Count > 0)
                {
                    ServiceCallEvent e = serviceCallQueue.Dequeue();
                    e.callback(e.info, e.response, e.exception);
                }
            }

            // Updates server and radar.
            server.Update();
            if (radar != null)
                radar.Update();
        }

        /// <summary>
        /// Retrieves the uOS device this app is running on.
        /// </summary>
        /// <returns></returns>
        public UpDevice GetCurrentDevice()
        {
            return deviceManager.currentDevice;
        }

        /// <summary>
        /// Lists all found devices so far.
        /// </summary>
        /// <returns></returns>
        public List<UpDevice> ListDevices()
        {
            return deviceManager.ListDevices();
        }

        public ChannelManager GetChannelManager(string networkDeviceType)
        {
            ChannelManager cm = null;
            if (channelManagers.TryGetValue(networkDeviceType, out cm))
                return cm;

            return null;
        }

        /// <summary>
        /// Calls a service and waits for response.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceCall"></param>
        /// <returns></returns>
        public Response CallService(UpDevice device, Call serviceCall)
        {
            // State variables to hold thread response...
            object _lock = new object();
            bool ready = false;
            System.Exception ex = null;
            Response res = null;

            // The thread that will call the service.
            var t = new Thread(new ThreadStart(delegate()
            {
                try
                {
                    // Calls the service with a callback that updates local variables...
                    CallService(
                        device,
                        serviceCall,
                        delegate(uOSServiceCallInfo info, Response r, System.Exception e2)
                        {
                            res = r;
                            lock (_lock)
                            {
                                ready = true;
                                ex = e2;
                            }
                        });
                }
                catch (System.Exception e)
                {
                    // If there were any exceptions, stores them...
                    lock (_lock)
                    {
                        ex = e;
                    }
                }
            }));

            // Starts the thread and busy waits for the response.
            t.Start();
            t.Join();

            // Did any exception happen?
            if (ex != null)
                throw ex;
            else
                while (!ready) ;

            // Returns response, if there were any.
            if (ex != null)
                throw ex;
            else
                return res;
        }

        /// <summary>
        /// Starts an asynchronous service call that will notify callback when any response is done.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceCall"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        public void CallService(
            UpDevice device,
            Call serviceCall,
            uOSServiceCallBack callback,
            object state = null
        )
        {
            if (
                    (serviceCall == null) ||
                    (serviceCall.driver == null) || (serviceCall.driver.Length == 0) ||
                    (serviceCall.service == null) || (serviceCall.service.Length == 0))
                throw new System.ArgumentException("Service Driver or Service Name is empty");

            StreamConnectionThreadData[] streamData = null;

            CallContext messageContext = new CallContext();
            messageContext.callerNetworkDevice = new LoopbackDevice(1);

            // In case of a Stream Service, a Stream Channel must be opened
            //if (serviceCall.serviceType == ServiceType.STREAM)
            //    streamData = OpenStreamChannel(device, serviceCall, messageContext);

            if (IsLocalCall(device))
                LocalServiceCall(serviceCall, streamData, messageContext, callback, state);
            else
                RemoteServiceCall(device, serviceCall, streamData, messageContext, callback, state);
        }

        public UpDevice RetrieveDevice(string networkAddress, string networkType)
        {
            return deviceManager.RetrieveDevice(networkAddress, networkType);
        }

        public void HandleNotify(Notify notify, UpDevice device)
        {
            if ((notify == null) || (notify.eventKey == null) || (notify.eventKey.Length == 0))
                logger.Log("No information in notify to handle.");

            if ((listenerMap == null) || (listenerMap.Count == 0))
            {
                logger.Log("No listeners waiting for notify events.");
                return;
            }

            //Notifying listeners from more specific to more general entries
            string eventIdentifier;
            List<ListenerInfo> listeners;

            // First full entries (device, driver, event, intanceId)
            eventIdentifier = GetEventIdentifier(device, notify.driver, notify.instanceId, notify.eventKey);
            if (listenerMap.TryGetValue(eventIdentifier, out listeners))
                HandleNotify(notify, listeners, eventIdentifier);

            // After less general entries (device, driver, event)
            eventIdentifier = GetEventIdentifier(device, notify.driver, null, notify.eventKey);
            if (listenerMap.TryGetValue(eventIdentifier, out listeners))
                HandleNotify(notify, listeners, eventIdentifier);

            // An then the least general entries (driver, event)
            eventIdentifier = GetEventIdentifier(null, notify.driver, null, notify.eventKey);
            if (listenerMap.TryGetValue(eventIdentifier, out listeners))
                HandleNotify(notify, listeners, eventIdentifier);
        }

        private void HandleNotify(Notify notify, List<ListenerInfo> listeners, string eventIdentifier)
        {
            if ((listeners == null) || (listeners.Count == 0))
            {
                logger.Log("No listeners waiting for notify events for the key '" + eventIdentifier + "'.");
                return;
            }

            foreach (ListenerInfo li in listeners)
            {
                if (li.listener != null)
                    li.listener.HandleEvent(notify);
            }
        }

        private static string GetEventIdentifier(UpDevice device, string driver, string instanceId, string eventKey)
        {
            StringBuilder id = new StringBuilder();

            if ((device != null) && (device.name != null) && (device.name.Length > 0))
                id.Append("@" + device.name);

            if ((driver != null) && (driver.Length > 0))
                id.Append("*" + driver);

            if ((eventKey != null) && (eventKey.Length > 0))
                id.Append("." + eventKey);

            if ((instanceId != null) && (instanceId.Length > 0))
                id.Append("#" + instanceId);

            return id.ToString();
        }

        public NetworkDevice GetAvailableNetworkDevice(string networkDeviceType)
        {
            ChannelManager cm = GetChannelManager(networkDeviceType);
            return cm == null ? null : cm.GetAvailableNetworkDevice();
        }

        public ClientConnection OpenActiveConnection(string networkDeviceName, string networkDeviceType)
        {
            ChannelManager cm = GetChannelManager(networkDeviceType);
            return cm == null ? null : cm.OpenActiveConnection(networkDeviceName);
        }

        public ClientConnection OpenPassiveConnection(string networkDeviceName, string networkDeviceType)
        {
            ChannelManager cm = GetChannelManager(networkDeviceType);
            return cm == null ? null : cm.OpenPassiveConnection(networkDeviceName);
        }

        public static IPAddress GetLocalIP()
        {
            return System.Array.Find<IPAddress>(
                Dns.GetHostEntry(Dns.GetHostName()).AddressList,
                a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
        }

        public static string GetHost(string networkDeviceName)
        {
            return networkDeviceName.Split(':')[0];
        }

        public static string GetChannelID(string networkDeviceName)
        {
            return networkDeviceName.Split(':')[1];
        }


        private void PrepareChannels()
        {
            IPAddress myIP = GetLocalIP();

            channelManagers = new Dictionary<string, ChannelManager>();
            channelManagers["Ethernet:UDP"] = new UDPChannelManager(myIP, settings.eth.udp.port, settings.eth.udp.passivePortRange);
            channelManagers["Ethernet:TCP"] = new TCPChannelManager(myIP, settings.eth.tcp.port, settings.eth.tcp.passivePortRange);
        }

        private void PrepareDeviceAndDrivers()
        {
            UpDevice currentDevice = new UpDevice();

            if (settings.deviceName != null)
            {
                string name = settings.deviceName.Trim();
                if (name.Length > 0)
                    currentDevice.name = name;
            }
            else
                currentDevice.name = Dns.GetHostName();

            if ((currentDevice.name == null) || (currentDevice.name == "localhost"))
                currentDevice.name = System.Environment.MachineName.ToLower() + "-unity";

            currentDevice.AddProperty("platform", "unity: " + Application.platform.ToString().ToLower());

            List<UpNetworkInterface> networks = new List<UpNetworkInterface>();
            foreach (ChannelManager cm in channelManagers.Values)
            {
                NetworkDevice nd = cm.GetAvailableNetworkDevice();
                UpNetworkInterface nInf = new UpNetworkInterface();
                nInf.netType = nd.networkDeviceType;
                nInf.networkAddress = GetHost(nd.networkDeviceName);
                networks.Add(nInf);
            }

            currentDevice.networks = networks;

            driverManager = new DriverManager(settings, this, currentDevice);
            deviceManager = new DeviceManager(settings, this, currentDevice);

            driverManager.InitDrivers();
        }

        private void PrepareServer()
        {
            server = new GatewayServer(this);
        }

        private void PrepareRadar()
        {
            switch (settings.radarType)
            {
                case RadarType.MULTICAST:
                    radar = new UnityMulticastRadar(logger);
                    break;

                default:
                    break;
            }

            if (radar != null)
                radar.DevicesChanged += OnRadarEvent;
        }

        private void PushEvent(uOSServiceCallBack callback, uOSServiceCallInfo info, Response r, System.Exception e)
        {
            PushEvent(new ServiceCallEvent { callback = callback, info = info, response = r, exception = e });
        }

        private void PushEvent(ServiceCallEvent e)
        {
            lock (_service_call_queue_lock)
            {
                serviceCallQueue.Enqueue(e);
            }
        }

        private void OnRadarEvent(object sender, RadarEvent type, SocketDevice device)
        {
            logger.Log(type.ToString() + ": " + device.networkDeviceName);
            switch (type)
            {
                case RadarEvent.DEVICE_ENTERED:
                    deviceManager.DeviceEntered(device);
                    break;

                case RadarEvent.DEVICE_LEFT:
                    deviceManager.DeviceLeft(device);
                    break;
            }
        }

        private bool IsLocalCall(UpDevice device)
        {
            return
                (device == null) ||
                (device.name == null) ||
                (device.name.Equals(GetCurrentDevice().name, System.StringComparison.InvariantCultureIgnoreCase));
        }

        private void RemoteServiceCall(
            UpDevice device,
            Call serviceCall,
            StreamConnectionThreadData[] streamData,
            CallContext messageContext,
            uOSServiceCallBack callback,
            object state)
        {
            UpNetworkInterface netInt = GetAppropriateInterface(device);
            ClientConnection con = OpenActiveConnection(netInt.networkAddress, netInt.netType);
            if (con == null)
                throw new System.Exception("Couldn't connect to target.");

            uOSServiceCallInfo info = new uOSServiceCallInfo { device = device, call = serviceCall, asyncState = state };
            string call = Json.Serialize(serviceCall.ToJSON()) + "\n";
            Debug.Log(call);
            byte[] data = Encoding.UTF8.GetBytes(call);

            con.WriteAsync(
                data,
                new ClientConnection.WriteCallback(
                    delegate(int bytesWriten, object callerState, System.Exception writeException)
                    {
                        if (writeException != null)
                        {
                            con.Close();

                            PushEvent(callback, info, null, writeException);
                        }
                        else
                        {
                            con.ReadAsync(
                                new ClientConnection.ReadCallback(
                                    delegate(byte[] bytes, object ce, System.Exception readException)
                                    {
                                        con.Close();

                                        Response r = null;
                                        string returnedMessage = null;
                                        if (readException == null)
                                        {
                                            if (bytes != null)
                                                returnedMessage = Encoding.UTF8.GetString(bytes);
                                            if (returnedMessage != null)
                                            {
                                                try
                                                {
                                                    r = Response.FromJSON(Json.Deserialize(returnedMessage));
                                                    r.messageContext = messageContext;
                                                }
                                                catch (System.Exception jsonException)
                                                {
                                                    readException = jsonException;
                                                }
                                            }
                                            else
                                                readException = new System.Exception("No response received from call.");
                                        }
                                        PushEvent(callback, info, r, readException);
                                    }
                                ),
                                callerState
                            );
                        }
                    }
                ),
                state
            );
        }

        private void LocalServiceCall(
            Call serviceCall,
            StreamConnectionThreadData[] streamData,
            CallContext messageContext,
            uOSServiceCallBack callback,
            object state)
        {
            return;

            logger.Log("Handling Local ServiceCall");

            try
            {
                Response response = null;

                NetworkDevice netDevice = messageContext.callerNetworkDevice;
                if (netDevice != null)
                {
                    string addr = GetHost(netDevice.networkDeviceName);
                    string type = netDevice.networkDeviceType;
                    messageContext.callerDevice = deviceManager.RetrieveDevice(addr, type);
                }

                //HandleDriverServiceCall(serviceCall, messageContext);

                response.messageContext = messageContext;

                //return response;
            }
            catch (System.Exception)
            {
                CloseStreams(streamData);
                throw;
            }
        }

        private void CloseStreams(StreamConnectionThreadData[] streamData)
        {
            if (streamData != null)
            {
                foreach (var s in streamData)
                    s.thread.Abort();
            }
        }

        private StreamConnectionThreadData[] OpenStreamChannel(UpDevice device, Call serviceCall, CallContext messageContext)
        {
            StreamConnectionThreadData[] data = null;

            //Channel type decision
            string netType = null;
            if (serviceCall.channelType != null)
                netType = serviceCall.channelType;
            else
            {
                UpNetworkInterface network = GetAppropriateInterface(device);
                netType = network.netType;
            }

            int channels = serviceCall.channels;
            data = new StreamConnectionThreadData[channels];
            string[] channelIDs = new string[channels];

            for (int i = 0; i < channels; i++)
            {
                NetworkDevice networkDevice = GetAvailableNetworkDevice(netType);
                channelIDs[i] = GetChannelID(networkDevice.networkDeviceName);
                StreamConnectionThreadData thread = new StreamConnectionThreadData(this, messageContext, networkDevice);
                thread.thread.Start();
                data[i] = thread;
            }

            serviceCall.channelIDs = channelIDs;
            serviceCall.channelType = netType;

            return data;
        }

        private UpNetworkInterface GetAppropriateInterface(UpDevice deviceProvider)
        {
            //List of compatible network interfaces
            List<UpNetworkInterface> compatibleNetworks = new List<UpNetworkInterface>();

            //Solves the different network link problem:
            foreach (UpNetworkInterface thisNetInterface in GetCurrentDevice().networks)
            {
                foreach (UpNetworkInterface providerNetInterface in deviceProvider.networks)
                {
                    if (thisNetInterface.netType.Equals(providerNetInterface.netType))
                    {
                        compatibleNetworks.Add(providerNetInterface);
                        break;
                    }
                }
            }

            //Checks if none compatible interface is available
            if (compatibleNetworks.Count == 0)
            {
                logger.LogError("ConnectivityManager - Lacks connectivity between the devices for this service");
                throw new System.Exception("ConnectivityManager - Lacks connectivity between the devices for this service");
            }

            //Gets the best choice of network for this service
            //UpNetworkInterface networkInterface = servicesBestInterface(compatibleNetworks, serviceCall);
            //return networkInterface;

            return compatibleNetworks[0];
        }

        /// <summary>
        /// Inner class for waiting a connection in case of stream service type.
        /// </summary>
        private class StreamConnectionThreadData
        {
            private UnityGateway gateway;

            public Thread thread { get; private set; }
            public CallContext msgContext { get; private set; }
            public NetworkDevice networkDevice { get; private set; }

            public StreamConnectionThreadData(UnityGateway gateway, CallContext msgContext, NetworkDevice networkDevice)
            {
                this.gateway = gateway;

                this.thread = new Thread(new ThreadStart(Run));
                this.msgContext = msgContext;
                this.networkDevice = networkDevice;
            }

            private void Run()
            {
                ClientConnection con = gateway.OpenPassiveConnection(networkDevice.networkDeviceName, networkDevice.networkDeviceType);
                msgContext.AddConnection(con);
            }
        }
    }

    class LoopbackDevice : NetworkDevice
    {
        private const string NETWORK_DEVICE_TYPE = "Loopback";
        protected const string DEVICE_NAME = "This Device";

        public long id { get; private set; }

        public override string networkDeviceName { get { return DEVICE_NAME + ":" + id; } }
        public override string networkDeviceType { get { return NETWORK_DEVICE_TYPE; } }

        public LoopbackDevice()
        {
            this.id = idCounter++;
        }

        /**
         * Instantiates a new LoopbackDevice with the given ID. This method infers the user knows
         * the given ID is a valid one.
         * @param The ID of the device
         */
        public LoopbackDevice(long id)
        {
            this.id = id;
        }

        private static long idCounter = 1;
        public static void initDevicesID()
        {
            idCounter = 1;
        }
    }
}
