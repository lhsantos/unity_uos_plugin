using System.Collections.Generic;
using System.Reflection;


namespace UOS
{
    public class DriverData
    {
        public string instanceID { get; private set; }
        public UpDriver driver { get; private set; }
        public UpDevice device { get; private set; }

        public DriverData(UpDriver driver, UpDevice device, string instanceID)
        {
            this.driver = driver;
            this.device = device;
            this.instanceID = instanceID;
        }

        public override bool Equals(object obj)
        {

            if ((obj != null) && (obj is DriverData))
            {
                DriverData temp = (DriverData)obj;
                if (temp.driver != null &&
                        temp.device != null &&
                        temp.instanceID != null)
                {

                    return temp.driver.Equals(this.driver) &&
                            temp.device.Equals(this.device) &&
                            temp.instanceID.Equals(this.instanceID);
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            if ((instanceID != null) && (driver != null) && (device != null))
                return instanceID.GetHashCode() ^ driver.GetHashCode() ^ device.GetHashCode();

            return base.GetHashCode();
        }
    }

    public class DriverManager
    {
        private uOSSettings settings;
        private UnityGateway gateway;
        private Logger logger;
        private IDictionary<string, IDictionary<string, UOSDriver>> drivers = new Dictionary<string, IDictionary<string, UOSDriver>>();
        private int instanceCounter = 0;

        public DriverManager(uOSSettings settings, UnityGateway gateway)
        {
            this.settings = settings;
            this.gateway = gateway;
            this.logger = gateway.logger;
        }

        public void DeployDriver(UpDriver driver, UOSDriver uDriver, string instanceId = null)
        {
            if (instanceId == null)
                instanceId = driver.name + instanceCounter++;

            IDictionary<string, UOSDriver> instances = null;
            if (!drivers.TryGetValue(driver.name, out instances))
            {
                instances = new Dictionary<string, UOSDriver>();
                drivers[driver.name] = instances;
            }

            instances[instanceId] = uDriver;
            uDriver.Init(gateway, settings, instanceId);

            logger.Log("Deployed Driver : " + driver.name + " with id " + instanceId);
        }

        public IList<DriverData> ListDrivers(string driverName, string deviceName)
        {
            IList<DriverData> list = new List<DriverData>();

            if (driverName != null)
            {
                IDictionary<string, UOSDriver> instances = null;
                if (drivers.TryGetValue(driverName, out instances))
                    AddDriversToList(list, driverName, instances);
            }
            else
            {
                foreach (var pair in drivers)
                    AddDriversToList(list, pair.Key, pair.Value);
            }

            return list;
        }

        public Response HandleServiceCall(Call serviceCall, CallContext messageContext)
        {
            IDictionary<string, UOSDriver> instances = null;
            if (drivers.TryGetValue(serviceCall.driver, out instances))
            {
                UOSDriver driver = null;
                if (serviceCall.instanceId != null)
                {
                    if (!instances.TryGetValue(serviceCall.instanceId, out driver))
                        throw new System.Exception("Couldn't find driver with instanceId " + serviceCall.instanceId);
                }
                else
                {
                    var i = instances.GetEnumerator();
                    i.MoveNext();
                    driver = i.Current.Value;
                }

                return CallServiceOnDriver(serviceCall, driver, messageContext);
            }
            else
                throw new System.Exception("Unknown driver " + serviceCall.driver);
        }

        private Response CallServiceOnDriver(Call serviceCall, UOSDriver instanceDriver, CallContext messageContext)
        {
            MethodInfo serviceMethod = FindMethod(serviceCall, instanceDriver);
            if (serviceMethod != null)
            {
                logger.Log(
                    "Calling service (" + serviceCall.service +
                    ") on Driver (" + serviceCall.driver +
                    ") in instance (" + serviceCall.instanceId + ")");

                HandleStreamCall(serviceCall, messageContext);

                Response response = new Response();
                serviceMethod.Invoke(instanceDriver, new object[] { serviceCall, response, messageContext });

                logger.Log("Finished service call.");
                return response;
            }
            else
                throw new System.Exception(
                    "No Service Implementation found for service '" + serviceCall.service +
                    "' on driver '" + serviceCall.driver +
                    "' with id '" + serviceCall.instanceId + "'");
        }

        private void HandleStreamCall(Call serviceCall, CallContext messageContext)
        {
            if (serviceCall.serviceType == ServiceType.STREAM)
            {
                NetworkDevice networkDevice = messageContext.callerNetworkDevice;

                string host = UnityGateway.GetHost(networkDevice.networkDeviceName);
                for (int i = 0; i < serviceCall.channels; i++)
                {
                    ClientConnection con = gateway.OpenActiveConnection(host + ":" + serviceCall.channelIDs[i], serviceCall.channelType);
                    messageContext.AddConnection(con);
                }
            }
        }

        private MethodInfo FindMethod(Call serviceCall, object instanceDriver)
        {
            string serviceName = serviceCall.service;

            foreach (var m in instanceDriver.GetType().GetMethods())
            {
                if (m.Name.Equals(serviceName, System.StringComparison.InvariantCultureIgnoreCase))
                    return m;
            }

            return null;
        }

        private void AddDriversToList(IList<DriverData> list, string driver, IDictionary<string, UOSDriver> instances)
        {
            foreach (var instance in instances)
                list.Add(new DriverData(instance.Value.GetDriver(), gateway.GetCurrentDevice(), instance.Key));
        }
    }
}
