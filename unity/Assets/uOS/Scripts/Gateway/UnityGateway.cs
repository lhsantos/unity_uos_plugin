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
        private const string DEVICE_DRIVER_NAME = "uos.DeviceDriver";
        private const string DRIVERS_NAME_KEY = "driversName";
        private const string INTERFACES_KEY = "interfaces";

        private uOSSettings settings;
        private Logger logger = new UnityLogger();
        private IDictionary<string, ChannelManager> channelManagers = null;
        private UpDevice currentDevice = null;
        private DeviceRegistry deviceRegistry = new DeviceRegistry();
        private object _device_reg_lock = new object();
        private IDictionary<string, object> localDrivers;
        private UnityNetworkRadar radar = null;
        //private DriverRegistry driverRegistry = new DriverRegistry();


        /// <summary>
        /// Creates a new Gateway with given settings.
        /// </summary>
        /// <param name="uOSSettings"></param>
        public UnityGateway(uOSSettings uOSSettings)
        {
            this.settings = uOSSettings;

            PrepareChannelManagers();
            PrepareDevice();
            PrepareDrivers();
            PrepareRadar();
        }

        /// <summary>
        /// Initialises this Gateway.
        /// </summary>
        public void Init()
        {
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
        }

        /// <summary>
        /// IUnityUpdatable update method, that will be called by uOS to process events
        /// inside Unity's thread.
        /// </summary>
        public void Update()
        {
            if (radar != null)
                radar.Update();
        }

        /// <summary>
        /// Retrieves the uOS device this app is running on.
        /// </summary>
        /// <returns></returns>
        public UpDevice GetCurrentDevice()
        {
            return currentDevice;
        }

        /// <summary>
        /// Lists all found devices so far.
        /// </summary>
        /// <returns></returns>
        public IList<UpDevice> ListDevices()
        {
            lock (_device_reg_lock)
            {
                return deviceRegistry.List();
            }
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


        private void PrepareChannelManagers()
        {
            IPAddress myIP = System.Array.Find<IPAddress>(
                Dns.GetHostEntry(Dns.GetHostName()).AddressList,
                a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            channelManagers = new Dictionary<string, ChannelManager>();
            channelManagers["Ethernet:UDP"] = new UDPChannelManager(myIP, settings.eth.udp.port);
            channelManagers["Ethernet:TCP"] = new TCPChannelManager(myIP, settings.eth.tcp.port);
        }

        private void PrepareDevice()
        {
            currentDevice = new UpDevice();

            if (settings.deviceName != null)
            {
                string name = settings.deviceName.Trim();
                if (name.Length > 0)
                    currentDevice.name = name;
            }
            else
                currentDevice.name = Dns.GetHostName();

            if ((currentDevice.name == null) || (currentDevice.name == "localhost"))
                currentDevice.name = "unity" + (new System.Random()).Next();

            currentDevice.AddProperty("platform", "unity: " + Application.platform.ToString().ToLower());

            IList<UpNetworkInterface> networks = new List<UpNetworkInterface>();
            foreach (ChannelManager cm in channelManagers.Values)
            {
                NetworkDevice nd = cm.GetAvailableNetworkDevice();
                UpNetworkInterface nInf = new UpNetworkInterface();
                nInf.netType = nd.networkDeviceType;
                nInf.networkAddress = GetHost(nd.networkDeviceName);
                networks.Add(nInf);
            }

            currentDevice.networks = networks;
        }

        private void PrepareDrivers()
        {
            localDrivers = new Dictionary<string, object>();
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

        private void OnRadarEvent(object sender, RadarEvent type, SocketDevice device)
        {
            logger.Log(type.ToString() + ": " + device.networkDeviceName);
            switch (type)
            {
                case RadarEvent.DEVICE_ENTERED:
                    DeviceEntered(device);
                    break;

                case RadarEvent.DEVICE_LEFT:
                    DeviceLeft(device);
                    break;
            }
        }

        private void DeviceEntered(SocketDevice device)
        {
            if (device == null)
                return;

            // verify if device entered is the current device
            string deviceHost = GetHost(device.networkDeviceName);
            foreach (UpNetworkInterface ni in currentDevice.networks)
            {
                string otherHost = ni.networkAddress;
                if ((deviceHost != null) && deviceHost.Equals(otherHost))
                {
                    logger.Log("Host of device entered is the same of current device:" + device.networkDeviceName);
                    return;
                }
            }

            // verify if already know this device.
            UpDevice upDevice = RetrieveDevice(deviceHost, device.networkDeviceType);

            if (upDevice == null)
                BeginHandshake(device, upDevice);
            //{
            //upDevice = DoHandshake(device, upDevice);
            //if (upDevice != null)
            //    DoDriversRegistry(device, upDevice);
            //}
            else
                logger.Log("Already known device " + device.networkDeviceName);
        }

        private void DeviceLeft(NetworkDevice device)
        {
        }

        private void RegisterDevice(UpDevice device)
        {
            lock (_device_reg_lock)
            {
                deviceRegistry.Add(device);
            }
        }

        private UpDevice RetrieveDevice(string deviceName)
        {
            lock (_device_reg_lock)
            {
                return deviceRegistry.Find(deviceName);
            }
        }

        private UpDevice RetrieveDevice(string networkAddress, string networkType)
        {
            IList<UpDevice> list = null;
            lock (_device_reg_lock)
            {
                list = deviceRegistry.List(networkAddress, networkType);
            }

            if (list != null && (list.Count > 0))
            {
                UpDevice deviceFound = list[0];
                logger.Log(
                    "Device with addr '" + networkAddress + "' found on network '" + networkType + "' resolved to " + deviceFound);

                return deviceFound;
            }

            logger.Log("No device found with addr '" + networkAddress + "' on network '" + networkType + "'.");

            return null;
        }
        //private void DoDriversRegistry(NetworkDevice device, UpDevice upDevice)
        //{
        //    Response response = CallService(upDevice, new Call(DEVICE_DRIVER_NAME, "listDrivers"));
        //    if ((response != null) && (response.responseData != null))
        //    {
        //        object temp = response.getResponseData("driverList");
        //        if (temp != null)
        //        {
        //            IDictionary<string, object> driversListMap = null;
        //            if (temp is IDictionary<string, object>)
        //                driversListMap = temp as IDictionary<string, object>;
        //            else
        //                driversListMap = Json.Deserialize(temp.ToString()) as IDictionary<string, object>;

        //            string[] ids = new string[driversListMap.Count];
        //            driversListMap.Keys.CopyTo(ids, 0);

        //            RegisterRemoteDriverInstances(upDevice, driversListMap, ids);
        //        }
        //    }
        //}

        //private void RegisterRemoteDriverInstances(UpDevice upDevice, IDictionary<string, object> driversListMap, string[] instanceIds)
        //{
        //    foreach (string id in instanceIds)
        //    {
        //        UpDriver upDriver = UpDriver.FromJSON(Json.Deserialize(driversListMap[id] as string));
        //        driverRegistry.Add(id, upDriver, upDevice.name);
        //    }
        //}

        private void BeginHandshake(NetworkDevice device, UpDevice upDevice)
        {
            // Create a Dummy device just for calling it
            logger.Log("Trying to hanshake with device : " + device.networkDeviceName);

            UpDevice dummyDevice = new UpDevice(device.networkDeviceName);
            dummyDevice.AddNetworkInterface(device.networkDeviceName, device.networkDeviceType);

            Call call = new Call(DEVICE_DRIVER_NAME, "handshake", null);
            call.AddParameter("device", Json.Serialize(currentDevice.ToJSON()));

            CallService(
                dummyDevice,
                call,
                delegate(uOSServiceCallInfo info, Response response, System.Exception e)
                {
                    if ((e != null) && (response != null) && (response.error == null || (response.error.Length == 0)))
                    {
                        // in case of a success greeting process, register the device in the neighborhood database
                        object responseDevice = response.getResponseData("device");
                        if (responseDevice != null)
                        {
                            UpDevice remoteDevice;
                            if (responseDevice is string)
                                remoteDevice = UpDevice.FromJSON(Json.Deserialize(responseDevice as string));
                            else
                                remoteDevice = UpDevice.FromJSON(responseDevice);

                            RegisterDevice(remoteDevice);
                        }
                        else
                            logger.LogError("Not possible complete handshake with device '" + device.networkDeviceName + "' for no device on the handshake response.");
                    }
                    else
                        logger.LogError(
                            "Not possible to handshake with device '" +
                            device.networkDeviceName +
                            ((e != null) ? (": " + e.Message) :
                                ((response == null) ? ": No Response received." : (": " + response.error)))
                        );
                }
            );
        }

        private bool IsLocalCall(UpDevice device)
        {
            return
                (device == null) ||
                (device.name == null) ||
                (device.name.Equals(currentDevice.name, System.StringComparison.InvariantCultureIgnoreCase));
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
            byte[] data = Encoding.UTF8.GetBytes(Json.Serialize(serviceCall.ToJSON()));

            con.WriteAsync(
                data,
                new ClientConnection.WriteCallback(
                    delegate(int bytesWriten, object callerState, System.Exception e)
                    {
                        if (e != null)
                            callback(info, null, e);
                        else
                        {
                            con.ReadAsync(
                                new ClientConnection.ReadCallback(
                                    delegate(byte[] bytes, object ce, System.Exception ex)
                                    {
                                        con.Close();

                                        Response r = null;
                                        string returnedMessage = null;
                                        if (ex == null)
                                        {
                                            if (bytes != null)
                                                returnedMessage = Encoding.UTF8.GetString(bytes);
                                            if (returnedMessage != null)
                                            {
                                                r = Response.FromJSON(Json.Deserialize(returnedMessage));
                                                r.messageContext = messageContext;
                                            }
                                            else
                                                ex = new System.Exception("No response received from call.");
                                        }

                                        callback(info, r, ex);
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
                    messageContext.callerDevice = RetrieveDevice(addr, type);
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
                    s.thread.Interrupt();
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
            IList<UpNetworkInterface> compatibleNetworks = new List<UpNetworkInterface>();

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

        public NetworkDevice GetAvailableNetworkDevice(string networkDeviceType)
        {
            ChannelManager channelManager = null;
            if (channelManagers.TryGetValue(networkDeviceType, out channelManager))
                return channelManager.GetAvailableNetworkDevice();

            return null;
        }

        private ClientConnection OpenActiveConnection(string networkDeviceName, string networkDeviceType)
        {
            return channelManagers[networkDeviceType].OpenActiveConnection(networkDeviceName);
        }

        private ClientConnection OpenPassiveConnection(string networkDeviceName, string networkDeviceType)
        {
            return channelManagers[networkDeviceType].OpenPassiveConnection(networkDeviceName);
        }

        private static string GetChannelID(string networkDeviceName)
        {
            return networkDeviceName.Split(':')[1];
        }

        private static string GetHost(string networkDeviceName)
        {
            return networkDeviceName.Split(':')[0];
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
