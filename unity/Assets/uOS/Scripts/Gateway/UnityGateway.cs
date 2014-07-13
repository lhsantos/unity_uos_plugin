using MiniJSON;
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
        public ReflectionServiceCaller reflectionServiceCaller { get; private set; }
        public UOSApplication app { get; set; }
        public DriverManager driverManager { get; private set; }
        public DeviceManager deviceManager { get; private set; }
        public UpDevice currentDevice { get { return deviceManager.currentDevice; } }


        /// <summary>
        /// Creates a new Gateway with given settings.
        /// </summary>
        /// <param name="uOSSettings"></param>
        public UnityGateway(uOSSettings uOSSettings, Logger logger, UOSApplication app = null)
        {
            this.settings = uOSSettings;
            this.logger = logger;
            this.reflectionServiceCaller = new ReflectionServiceCaller(uOSSettings, this);
            this.app = app;
        }

        /// <summary>
        /// Initialises this Gateway.
        /// </summary>
        public void Init()
        {
            PrepareChannels();
            PrepareDeviceAndDrivers();
            PrepareServer();
            PrepareRadar();

            if (app != null)
                app.Init(this, settings);

            server.Init();

            if (radar != null)
                radar.Init();
        }

        /// <summary>
        /// Releases this Gateway.
        /// </summary>
        public void TearDown()
        {
            if (radar != null)
            {
                radar.TearDown();
                radar = null;
            }

            server.TearDown();
            server = null;

            foreach (ChannelManager cm in channelManagers.Values)
                cm.TearDown();
            channelManagers.Clear();
            channelManagers = null;

            if (app != null)
                app.TearDown();
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
        /// Lists all drivers found so far.
        /// </summary>
        /// <param name="driverName"></param>
        /// <returns></returns>
        public List<DriverData> ListDrivers(string driverName)
        {
            return driverManager.ListDrivers(driverName, null);
        }

        /// <summary>
        /// Lists all found devices so far.
        /// </summary>
        /// <returns></returns>
        public List<UpDevice> ListDevices()
        {
            return deviceManager.ListDevices();
        }

        public UpDevice RetrieveDevice(string networkAddress, string networkType)
        {
            return deviceManager.RetrieveDevice(networkAddress, networkType);
        }

        public ChannelManager GetChannelManager(string networkDeviceType)
        {
            ChannelManager cm = null;
            if (channelManagers.TryGetValue(networkDeviceType, out cm))
                return cm;

            return null;
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

        /// <summary>
        /// Starts an asynchronous service call that will notify callback when any response is done.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceCall"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        public void CallServiceAsync(
            UpDevice device,
            Call serviceCall,
            uOSServiceCallBack callback,
            object state = null
        )
        {
            uOSServiceCallInfo info = new uOSServiceCallInfo { device = device, call = serviceCall, asyncState = state };
            new Thread(new ThreadStart(
                delegate()
                {
                    try
                    {
                        Response r = CallService(device, serviceCall);
                        PushEvent(callback, info, r, null);
                    }
                    catch (System.Exception e) { PushEvent(callback, info, null, e); }
                })
            ).Start();
        }

        /// <summary>
        /// Calls a service and waits for response.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceCall"></param>
        /// <returns></returns>
        public Response CallService(UpDevice device, Call serviceCall)
        {
            if (
                    (serviceCall == null) ||
                    (serviceCall.driver == null) || (serviceCall.driver.Length == 0) ||
                    (serviceCall.service == null) || (serviceCall.service.Length == 0))
                throw new System.ArgumentException("Service Driver or Service Name is empty");

            StreamConnectionThreadData[] streamConData = null;

            CallContext messageContext = new CallContext();

            messageContext.callerNetworkDevice = new LoopbackDevice(1);

            // In case of a Stream Service, a Stream Channel must be opened
            if (serviceCall.serviceType == ServiceType.STREAM)
                streamConData = OpenStreamChannel(device, serviceCall, messageContext);

            if (IsLocalCall(device))
                return LocalServiceCall(serviceCall, streamConData, messageContext);
            else
                return RemoteServiceCall(device, serviceCall, streamConData, messageContext);
        }

        private bool IsLocalCall(UpDevice device)
        {
            return
                (device == null) ||
                (device.name == null) ||
                (device.name.Equals(currentDevice.name, System.StringComparison.InvariantCultureIgnoreCase));
        }

        private Response RemoteServiceCall(
            UpDevice target,
            Call serviceCall,
            StreamConnectionThreadData[] streamData,
            CallContext messageContext)
        {
            try
            {
                logger.Log("Call service on " + target.name + ": " + Json.Serialize(serviceCall.ToJSON()));
                // Encodes and sends call message.
                string msg = Json.Serialize(serviceCall.ToJSON()) + "\n";
                Response r;
                string responseMsg = SendMessage(msg, target);
                if (responseMsg != null)
                {
                    r = Response.FromJSON(Json.Deserialize(responseMsg));
                    r.messageContext = messageContext;
                    return r;
                }
                else
                    throw new System.Exception("No response received from call.");
            }
            catch (System.Exception e)
            {
                logger.LogError("Error on remote service call: " + e.ToString());
                CloseStreams(streamData);
                throw new ServiceCallException(e);
            }
        }

        private string SendMessage(string msg, UpDevice target, bool waitForResponse = true)
        {
            UpNetworkInterface netInt = GetAppropriateInterface(target);
            string networkAddress = netInt.networkAddress;
            string networkType = netInt.netType;

            ClientConnection con = OpenActiveConnection(networkAddress, networkType);
            if (con == null)
            {
                logger.LogWarning("Not possible to stablish a connection with '" + networkAddress + "' of type '" + networkType + "'.");
                return null;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(msg);
                con.Write(data, 0, data.Length);


                // Gets response.
                string response = null;
                if (waitForResponse)
                {
                    data = con.Read();
                    if (data != null)
                    {
                        response = Encoding.UTF8.GetString(data);
                        if (response.Trim().Length == 0)
                            response = null;
                    }
                }
                con.Close();
                return response;
            }
            catch (System.Exception)
            {
                if (con.connected)
                    con.Close();
                throw;
            }
        }

        private Response LocalServiceCall(
            Call serviceCall,
            StreamConnectionThreadData[] streamData,
            CallContext messageContext)
        {
            logger.Log("Handling Local ServiceCall");

            try
            {
                Response response = HandleServiceCall(serviceCall, messageContext);
                response.messageContext = messageContext;

                return response;
            }
            catch (System.Exception e)
            {
                // if there was an opened stream channel, it must be closed
                CloseStreams(streamData);
                throw new ServiceCallException(e);
            }
        }

        public Response HandleServiceCall(Call serviceCall, CallContext messageContext)
        {
            NetworkDevice netDevice = messageContext.callerNetworkDevice;
            if (netDevice != null)
            {
                if (netDevice is LoopbackDevice)
                    messageContext.callerDevice = currentDevice;
                else
                {
                    string addr = Util.GetHost(netDevice.networkDeviceName);
                    string type = netDevice.networkDeviceType;
                    messageContext.callerDevice = deviceManager.RetrieveDevice(addr, type);
                }
            }

            if (IsApplicationCall(serviceCall))
            {
                if (app == null)
                    throw new System.InvalidOperationException("No valid app instance set.");
                return reflectionServiceCaller.CallService(app, serviceCall, messageContext);
            }
            else
                return driverManager.HandleServiceCall(serviceCall, messageContext);
        }

        private bool IsApplicationCall(Call serviceCall)
        {
            return (serviceCall.driver != null) && serviceCall.driver.Equals("app", System.StringComparison.InvariantCultureIgnoreCase);
        }

        public void Register(
            UOSEventListener listener,
            UpDevice device,
            string driver,
            string instanceId = null,
            string eventKey = null,
            IDictionary<string, object> parameters = null)
        {
            // If the listener is already registered it cannot be registered again
            string eventIdentifier = GetEventIdentifier(device, driver, instanceId, eventKey);
            logger.Log("Registering listener for event :" + eventIdentifier);

            List<ListenerInfo> list;
            if (!listenerMap.TryGetValue(eventIdentifier, out list))
                list = null;

            if (FindListener(listener, list) == null)
            {
                ListenerInfo info = new ListenerInfo();

                info.driver = driver;
                info.instanceId = instanceId;
                info.eventKey = eventKey;
                info.listener = listener;
                info.device = device;

                RegisterNewListener(device, parameters, eventIdentifier, info);
            }
        }

        /// <summary>
        /// Removes a listener for receiving Notify events and notifies the event driver of its removal.
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="device"></param>
        /// <param name="driver"></param>
        /// <param name="instanceId"></param>
        /// <param name="eventKey"></param>
        public void Unregister(
            UOSEventListener listener,
            UpDevice device = null,
            string driver = null,
            string instanceId = null,
            string eventKey = null)
        {
            List<ListenerInfo> listeners = FindListeners(device, driver, instanceId, eventKey);
            if (listeners == null)
                return;

            System.Exception e = null;
            foreach (var li in listeners)
            {
                // only if its the same listener, it should be removed
                if (li.listener.Equals(listener))
                {
                    bool remove = true;

                    // If the driver name is informed, and it's not the same, it must not be removed
                    if ((driver != null) && (li.driver != null))
                        remove = remove && li.driver.Equals(driver, System.StringComparison.InvariantCultureIgnoreCase);

                    // If the instanceId is informed, and it's not the same, it must not be removed
                    if ((instanceId != null) && (li.instanceId != null))
                        remove = remove && li.instanceId.Equals(instanceId, System.StringComparison.InvariantCultureIgnoreCase);

                    if (remove)
                    {
                        try
                        {
                            //Notify device of the listener removal
                            UnregisterForEvent(li);

                        }
                        catch (System.Exception ex)
                        {
                            string id = GetEventIdentifier(device, driver, instanceId, eventKey);
                            logger.LogError("Failed to unregister for event " + id + ": " + e.Message);
                            e = ex;
                        }
                    }
                }
            }

            if (e != null)
                throw e;
        }

        private void UnregisterForEvent(ListenerInfo listenerInfo)
        {
            // Send the event register request to the called device
            Call call = new Call(listenerInfo.driver, UNREGISTER_LISTENER_SERVICE, listenerInfo.instanceId);
            call.AddParameter(REGISTER_EVENT_LISTENER_EVENT_KEY_PARAMETER, listenerInfo.eventKey);

            Response response = CallService(listenerInfo.device, call);
            if (response == null)
                throw new ServiceCallException("No response receive from unregister service call.");
            if (!string.IsNullOrEmpty(response.error))
                throw new ServiceCallException(response.error);
        }

        public void Notify(Notify notify, UpDevice device)
        {
            if (IsLocalCall(device))
                HandleNotify(notify, device);
            else
                RemoteNotify(notify, device);
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

        private List<ListenerInfo> FindListeners(UpDevice device, string driver, string instanceId, string eventKey)
        {
            List<ListenerInfo> listeners = null;

            // First filter the listeners by the event key.
            if (eventKey == null)
            {
                // In this case all eventKeys must be checked for the listener to be removed.
                listeners = new List<ListenerInfo>();
                foreach (var list in listenerMap.Values)
                    listeners.AddRange(list);
            }
            else
            {
                // In case a eventKey is informed, then only the listeners for that event key must be used.
                string eventIdentifier = GetEventIdentifier(device, driver, instanceId, eventKey);
                if (!listenerMap.TryGetValue(eventIdentifier, out listeners))
                    listeners = null;
            }

            return listeners;
        }

        private ListenerInfo? FindListener(UOSEventListener listener, List<ListenerInfo> list)
        {
            if ((list != null) && (listener != null))
            {
                int pos = list.FindIndex(i => i.listener.Equals(listener));
                if (pos >= 0)
                    return list[pos];
            }

            return null;
        }

        private void RegisterNewListener(
            UpDevice device,
            IDictionary<string, object> parameters,
            string eventIdentifier,
            ListenerInfo info)
        {
            if (device != null)
                SendRegister(device, parameters, info);

            // If the registry process goes ok, then add the listenner to the listener map
            List<ListenerInfo> listeners = null;
            if (!listenerMap.TryGetValue(eventIdentifier, out listeners))
                listenerMap[eventIdentifier] = listeners = new List<ListenerInfo>();
            listeners.Add(info);
            logger.Log("Registered listener for event :" + eventIdentifier);
        }

        private void SendRegister(UpDevice device, IDictionary<string, object> parameters, ListenerInfo info)
        {
            // Send the event register request to the called device
            Call serviceCall = new Call(info.driver, REGISTER_LISTENER_SERVICE, info.instanceId);
            serviceCall.AddParameter(REGISTER_EVENT_LISTENER_EVENT_KEY_PARAMETER, info.eventKey);
            if (parameters != null)
            {
                foreach (var pair in parameters)
                {
                    if (pair.Key.Equals(REGISTER_EVENT_LISTENER_EVENT_KEY_PARAMETER, System.StringComparison.InvariantCultureIgnoreCase))
                        throw new System.ArgumentException("Can't use reserved keys as parameters for registerForEvent");
                    serviceCall.AddParameter(pair.Key, pair.Value);
                }
            }

            Response response = CallService(device, serviceCall);
            if (response == null)
                throw new System.Exception("No response received during register process.");
            else if (!string.IsNullOrEmpty(response.error))
                throw new System.Exception(response.error);
        }

        private Call BuildRegisterCall(IDictionary<string, object> parameters, ListenerInfo info)
        {
            Call serviceCall = new Call(info.driver, REGISTER_LISTENER_SERVICE, info.instanceId);
            serviceCall.AddParameter(REGISTER_EVENT_LISTENER_EVENT_KEY_PARAMETER, info.eventKey);
            if (parameters != null)
            {
                foreach (var pair in parameters)
                {
                    if (pair.Key.Equals(REGISTER_EVENT_LISTENER_EVENT_KEY_PARAMETER, System.StringComparison.InvariantCultureIgnoreCase))
                        throw new System.ArgumentException("Can't use reserved keys as parameters for registerForEvent");
                    serviceCall.AddParameter(pair.Key, pair.Value);
                }
            }
            return serviceCall;
        }

        private void RemoteNotify(Notify notify, UpDevice device)
        {
            if (
                    device == null || notify == null ||
                    string.IsNullOrEmpty(notify.driver) ||
                    string.IsNullOrEmpty(notify.eventKey))
                throw new System.ArgumentException("Either the device or notification is invalid.");

            string message = Json.Serialize(notify.ToJSON());
            SendMessage(message, device, false);
        }

        private void PrepareChannels()
        {
            channelManagers = new Dictionary<string, ChannelManager>();
            channelManagers["Ethernet:TCP"] = new TCPChannelManager(settings.eth.tcp.port, settings.eth.tcp.passivePortRange);
            //channelManagers["WebSocket"] = new WebSocketChannelManager(settings.websocket.hostName, settings.websocket.port, settings.websocket.timeout);
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

            if ((currentDevice.name == null) || (currentDevice.name.ToLower() == "localhost"))
                currentDevice.name = SystemInfo.deviceName;
            if ((currentDevice.name == null) || currentDevice.name.ToLower().Contains("unknown"))
                currentDevice.name = System.Environment.UserName;

            currentDevice.AddProperty("platform", "unity-" + Application.platform.ToString().ToLower());

            var networks = new List<UpNetworkInterface>();
            foreach (ChannelManager cm in channelManagers.Values)
            {
                foreach (var host in cm.ListHosts())
                {
                    var nInf = new UpNetworkInterface();
                    nInf.netType = cm.GetNetworkDeviceType();
                    nInf.networkAddress = host;
                    networks.Add(nInf);
                }
            }

            currentDevice.networks = networks;

            driverManager = new DriverManager(settings, this, currentDevice);
            deviceManager = new DeviceManager(settings, this, currentDevice);

            foreach (var driver in settings.drivers)
            {
                System.Type type = null;
                try { type = Util.GetType(driver); }
                catch (System.Exception) { }

                string error = null;
                object instance = null;
                if ((type == null) || (type.GetInterface("UOS.UOSDriver") == null) || (!type.IsClass) || (type.IsAbstract))
                    error = driver + " is not a valid concrete type which implements UOSDriver interface";
                else
                {
                    if (type.IsSubclassOf(typeof(MonoBehaviour)))
                    {
                        object[] components = UnityEngine.Object.FindObjectsOfType(type);
                        if (components.Length == 0)
                            error = "no instance of MonoBehaviour " + driver + " was found in the scene";
                        else if (components.Length > 1)
                            error = "multiple instances of MonoBehaviour " + driver + " were found in the scene";
                        else
                            instance = components[0];
                    }
                    else
                    {
                        try { instance = System.Activator.CreateInstance(type); }
                        catch (System.Reflection.TargetInvocationException e)
                        {
                            error = "constructor exception: " + e.InnerException;
                        }
                        catch (System.Exception)
                        {
                            error = "couldn't instantiate " + driver + " using default constructor";
                        }
                    }
                }

                if (error != null)
                    logger.LogError("Driver initialisation failure: " + error + ".");
                else
                {
                    var driverInstance = (UOSDriver)instance;
                    driverManager.DeployDriver(driverInstance);
                }
            }
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
                channelIDs[i] = Util.GetPort(networkDevice.networkDeviceName);
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
            foreach (UpNetworkInterface thisNetInterface in currentDevice.networks)
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
