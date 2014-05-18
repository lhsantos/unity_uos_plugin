using MiniJSON;
using System.Collections.Generic;


namespace UOS
{
    public class DeviceManager
    {
        private const string DEVICE_DRIVER_NAME = "uos.DeviceDriver";
        private const string DRIVERS_NAME_KEY = "driversName";
        private const string INTERFACES_KEY = "interfaces";

        private Logger logger = null;
        private UnityGateway gateway;
        private DriverManager driverManager;
        private object _devicedao_lock = new object();

        private HashSet<string> unknownDrivers;
        private HashSet<DriverModel> dependents;

        public DeviceDAO deviceDao { get; private set; }
        public UpDevice currentDevice { get; private set; }

        public DeviceManager(
                uOSSettings settings,
                UnityGateway gateway,
                UpDevice currentDevice)
        {
            this.gateway = gateway;
            this.logger = gateway.logger;
            this.currentDevice = currentDevice;
            this.deviceDao = new DeviceDAO();
            this.deviceDao.Add(currentDevice);
            this.driverManager = gateway.driverManager;
            this.unknownDrivers = new HashSet<string>();
            this.dependents = new HashSet<DriverModel>();
        }


        /// <summary>
        /// Registers a device in the neighborhood of the current device.
        /// </summary>
        /// <param name="device">Device to be registered.</param>
        public void RegisterDevice(UpDevice device)
        {
            lock (_devicedao_lock)
            {
                deviceDao.Add(device);
            }
        }


        /// <summary>
        /// Finds data about a device present in the neighborhood.
        /// </summary>
        /// <param name="deviceName">Device name to be found.</param>
        /// <returns>The device, if found; null, otherwise.</returns>
        public UpDevice RetrieveDevice(string deviceName)
        {
            lock (_devicedao_lock)
            {
                return deviceDao.Find(deviceName);
            }
        }

        /// <summary>
        /// Finds data about a device present in the neighborhood.
        /// </summary>
        /// <param name="networkAddress">Address of the Device to be found.</param>
        /// <param name="networkType">NetworkType of Address of the Device to be found.</param>
        /// <returns>The device, if found; null, otherwise.</returns>
        public UpDevice RetrieveDevice(string networkAddress, string networkType)
        {
            List<UpDevice> list;
            lock (_devicedao_lock) { list = deviceDao.List(networkAddress, networkType); }

            if ((list != null) && (list.Count > 0))
            {
                UpDevice foundDevice = list[0];
                logger.Log("Device with addr '" + networkAddress + "' found on network '" + networkType + "' resolved to " + foundDevice);
                return foundDevice;
            }
            logger.Log("No device found with addr '" + networkAddress + "' on network '" + networkType + "'.");

            return null;
        }


        /// <summary>
        /// Called by radar whenever a device enters the space.
        /// </summary>
        /// <param name="device"></param>
        public void DeviceEntered(NetworkDevice device)
        {
            if (device == null)
                return;

            // verify if device entered is the current device
            string deviceHost = UnityGateway.GetHost(device.networkDeviceName);
            foreach (UpNetworkInterface networkInterface in this.currentDevice.networks)
            {
                string currentDeviceHost = UnityGateway.GetHost(networkInterface.networkAddress);
                if (deviceHost != null && deviceHost.Equals(currentDeviceHost))
                {
                    logger.Log("Host of device entered is the same of current device:" + device.networkDeviceName);
                    return;
                }
            }

            // verify if already know this device.
            UpDevice upDevice = RetrieveDevice(deviceHost, device.networkDeviceType);

            if (upDevice == null)
                BeginHandshake(device);
            else
                logger.Log("Already known device " + device.networkDeviceName);
        }

        private void BeginHandshake(NetworkDevice device)
        {
            // Create a Dummy device just for calling it
            logger.Log("Trying to hanshake with device : " + device.networkDeviceName);

            UpDevice dummyDevice = new UpDevice(device.networkDeviceName);
            dummyDevice.AddNetworkInterface(device.networkDeviceName, device.networkDeviceType);

            Call call = new Call(DEVICE_DRIVER_NAME, "handshake", null);
            call.AddParameter("device", Json.Serialize(currentDevice.ToJSON()));

            gateway.CallService(
                dummyDevice,
                call,
                delegate(uOSServiceCallInfo info, Response response, System.Exception e)
                {
                    if ((e == null) && (response != null) && ((response.error == null) || (response.error.Length == 0)))
                    {
                        // in case of a success greeting process, register the device in the neighborhood database
                        object responseDevice = response.GetResponseData("device");
                        if (responseDevice != null)
                        {
                            UpDevice upDevice;
                            if (responseDevice is string)
                                upDevice = UpDevice.FromJSON(Json.Deserialize(responseDevice as string));
                            else
                                upDevice = UpDevice.FromJSON(responseDevice);

                            RegisterDevice(upDevice);
                            BeginRegisterDrivers(device, upDevice);

                            logger.Log("Successfully handshaked with device '" + device.networkDeviceName + "'.");
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

        private void BeginRegisterDrivers(NetworkDevice device, UpDevice upDevice)
        {
            Call call = new Call(DEVICE_DRIVER_NAME, "listDrivers");
            gateway.CallService(
                upDevice,
                call,
                delegate(uOSServiceCallInfo info, Response response, System.Exception e)
                {
                    try
                    {
                        if (e != null)
                            throw e;
                        if ((response.error != null) && (response.error.Length > 0))
                            throw new System.Exception(response.error);

                        object temp = response.GetResponseData("driverList");
                        if (temp != null)
                        {
                            IDictionary<string, object> driversListMap = null;
                            if (temp is IDictionary<string, object>)
                                driversListMap = (IDictionary<string, object>)temp; //TODO: Not tested. Why?
                            else
                                driversListMap = (IDictionary<string, object>)Json.Deserialize(temp.ToString());

                            List<string> ids = new List<string>(driversListMap.Keys);
                            RegisterRemoteDriverInstances(upDevice, driversListMap, ids.ToArray());
                        }
                    }
                    catch (System.NullReferenceException) { /* null parameters, do nothing */ }
                    catch (System.Exception)
                    {
                        logger.LogError(
                            "Failed to list drivers for device " + upDevice.name +
                            " on interface " + device.networkDeviceName);
                    }
                }
            );
        }

        private void RegisterRemoteDriverInstances(UpDevice upDevice, IDictionary<string, object> driversListMap, string[] instanceIds)
        {
            foreach (string id in instanceIds)
            {
                UpDriver upDriver = UpDriver.FromJSON(Json.Deserialize(driversListMap[id].ToString()));
                DriverModel driverModel = new DriverModel(id, upDriver, upDevice.name);

                try
                {
                    driverManager.Insert(driverModel);
                }
                catch (DriverNotFoundException e)
                {
                    unknownDrivers.UnionWith(e.driversNames);
                    dependents.Add(driverModel);

                }
                catch (System.Exception)
                {
                    logger.LogError(
                        "Problems ocurred in the registering of driver '" + upDriver.name +
                        "' with instanceId '" + id + "' in the device '" + upDevice.name +
                        "' and it will not be registered.");
                }
            }

            if (unknownDrivers.Count > 0)
                BeginFindDrivers(unknownDrivers, upDevice);
        }

        private void BeginFindDrivers(HashSet<string> unknownDrivers, UpDevice upDevice)
        {
            Call call = new Call(DEVICE_DRIVER_NAME, "tellEquivalentDrivers", null);
            call.AddParameter(DRIVERS_NAME_KEY, Json.Serialize(new List<string>(unknownDrivers)));

            gateway.CallService(
                upDevice,
                call,
                delegate(uOSServiceCallInfo info, Response response, System.Exception e)
                {
                    try
                    {
                        if (e != null)
                            throw e;
                        if ((response.error != null) && (response.error.Length > 0))
                            throw new System.Exception(response.error);


                        string interfaces = response.GetResponseString(INTERFACES_KEY);
                        if (interfaces != null)
                        {
                            List<UpDriver> drivers = new List<UpDriver>();
                            List<object> interfacesJson = Json.Deserialize(interfaces) as List<object>;

                            for (int i = 0; i < interfacesJson.Count; i++)
                            {
                                UpDriver upDriver = UpDriver.FromJSON(Json.Deserialize(interfacesJson[i] as string));
                                drivers.Add(upDriver);
                            }

                            try
                            {
                                driverManager.AddToEquivalenceTree(drivers);
                            }
                            catch (InterfaceValidationException)
                            {
                                logger.LogError("Not possible to add to equivalance tree due to wrong interface specification.");
                            }

                            foreach (DriverModel dependent in dependents)
                            {
                                try
                                {
                                    driverManager.Insert(dependent);
                                }
                                catch (DriverNotFoundException)
                                {
                                    logger.LogError(
                                        "Not possible to register driver '" +
                                        dependent.driver.name + "' due to unkwnown equivalent driver.");
                                }
                                catch (System.Exception)
                                {
                                    logger.LogError(
                                        "Problems ocurred in the registering of driver '" +
                                        dependent.driver.name + "' with instanceId '" + dependent.id +
                                        "' in the device '" + upDevice.name + "' and it will not be registered.");
                                }
                            }
                        }
                        else
                            logger.LogError(
                                "Not possible to call service on device '" + upDevice.name +
                                "' for no equivalent drivers on the service response.");
                    }
                    catch (System.NullReferenceException) { /* null parameters, do nothing */ }
                    catch (System.Exception ex)
                    {
                        logger.LogError(
                            "Not possible to call service on device '" +
                            upDevice.name + "': Cause : " + ex.Message);
                    }
                }
            );
        }


        /// <summary>
        /// Called by radar whenever a device leaves the space.
        /// </summary>
        /// <param name="device"></param>
        public void DeviceLeft(NetworkDevice device)
        {
            if (device == null || device.networkDeviceName == null || device.networkDeviceType == null)
                return;

            // Remove what services this device has.
            logger.Log("Device " + device.networkDeviceName + " of type " + device.networkDeviceType + " leaving.");

            string host = UnityGateway.GetHost(device.networkDeviceName);
            List<UpDevice> devices;
            lock (_devicedao_lock) { devices = deviceDao.List(host, device.networkDeviceType); }

            if ((devices != null) && (devices.Count > 0))
            {
                UpDevice upDevice = devices[0];
                List<DriverModel> returnedDrivers = driverManager.List(null, upDevice.name);
                if ((returnedDrivers != null) && (returnedDrivers.Count > 0))
                {
                    foreach (DriverModel rdd in returnedDrivers)
                        driverManager.Delete(rdd.id, rdd.device);
                }

                lock (_devicedao_lock) { deviceDao.Delete(upDevice.name); };

                logger.Log("Device '" + upDevice.name + "' left");
            }
            else
                logger.Log("Device not found in database.");
        }

        public List<UpDevice> ListDevices()
        {
            lock (_devicedao_lock)
            {
                return deviceDao.List();
            }
        }
    }
}
