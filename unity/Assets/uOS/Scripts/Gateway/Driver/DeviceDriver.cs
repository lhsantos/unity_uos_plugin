using MiniJSON;
using System.Collections.Generic;


namespace UOS
{
    public class DeviceDriver : UOSDriver
    {
        private const string DEVICE_KEY = "device";
        //private const string SECURITY_TYPE_KEY = "securityType";
        private const string DRIVER_LIST_KEY = "driverList";
        private const string DRIVER_NAME_KEY = "driverName";
        private const string INTERFACES_KEY = "interfaces";
        private const string DRIVERS_NAME_KEY = "driversName";

        private readonly UpDriver driver;

        private UnityGateway gateway;
        private Logger logger;


        public DeviceDriver()
        {
            driver = new UpDriver("uos.DeviceDriver");

            driver.AddService("listDrivers")
                .AddParameter(DRIVER_NAME_KEY, UpService.ParameterType.OPTIONAL);

            //driver.AddService("authenticate")
            //    .AddParameter(SECURITY_TYPE_KEY, UpService.ParameterType.MANDATORY);

            driver.AddService("goodbye");

            driver.AddService("handshake")
                .AddParameter(DEVICE_KEY, UpService.ParameterType.MANDATORY);

            driver.AddService("tellEquivalentDriver")
                .AddParameter(DRIVER_NAME_KEY, UpService.ParameterType.MANDATORY);
        }

        public UpDriver GetDriver()
        {
            return driver;
        }

        public List<UpDriver> GetParent()
        {
            return null;
        }

        public void Init(IGateway gateway, uOSSettings settings, string instanceId)
        {
            this.gateway = (UnityGateway)gateway;
            this.logger = this.gateway.logger;
        }

        public void Destroy()
        {
        }

        public void ListDrivers(Call serviceCall, Response serviceResponse, CallContext messageContext)
        {
            logger.Log("Handling DeviceDriverImpl#listDrivers service");

            try
            {
                IDictionary<string, object> parameters = serviceCall.parameters;
                DriverManager driverManager = gateway.driverManager;

                // Handles parameters to filter message...
                IList<DriverData> listDrivers =
                    driverManager.ListDrivers(
                        ((parameters != null) ? (parameters[DRIVER_NAME_KEY] as string) : null),
                        gateway.currentDevice.name
                    );

                IDictionary<string, object> driversList = new Dictionary<string, object>();
                if ((listDrivers != null) && (listDrivers.Count > 0))
                {
                    foreach (var driver in listDrivers)
                        driversList[driver.instanceID] = driver.driver.ToJSON();
                }

                IDictionary<string, object> responseData = new Dictionary<string, object>();
                responseData[DRIVER_LIST_KEY] = driversList;
                serviceResponse.responseData = responseData;
            }
            catch (System.Exception e)
            {
                serviceResponse.error = e.Message;
                logger.LogError("Problem on ListDrivers service: " + e.Message + "," + e.StackTrace);
            }
        }

        public void Handshake(Call serviceCall, Response serviceResponse, CallContext messageContext)
        {
            string deviceParameter = serviceCall.GetParameterString(DEVICE_KEY);

            if (deviceParameter == null)
            {
                serviceResponse.error = "No 'device' parameter informed.";
                return;
            }

            try
            {
                UpDevice device = UpDevice.FromJSON(Json.Deserialize(deviceParameter));

                gateway.deviceManager.RegisterDevice(device);

                serviceResponse.AddParameter(DEVICE_KEY, Json.Serialize(gateway.currentDevice.ToJSON()));

                //TODO: actually implement the driver register for other devices...
                //Response driversResponse = gateway.CallService(device, new Call("uos.DeviceDriver", "listDrivers"));
                //object driverList = driversResponse.GetResponseData("driverList");
                //if (driverList != null)
                //{
                //    var driverMap = (IDictionary<string, object>)Json.Deserialize(driverList.ToString());
                //    // TODO: this is duplicated with DeviceManager.registerRemoteDriverInstances
                //    foreach (string id in driverMap.Keys)
                //    {
                //        UpDriver upDriver = UpDriver.FromJSON(Json.Deserialize(driverMap[id].ToString()));
                //        DriverModel driverModel = new DriverModel(id, upDriver, device.name);
                //        gateway.driverManager.Insert(driverModel);
                //    }
                //}
            }
            catch (System.Exception e)
            {
                serviceResponse.error = e.Message;
                logger.LogError("Problems on handshake: " + e.Message + "," + e.StackTrace);
            }
        }

        public void Goodbye(Call serviceCall, Response serviceResponse, CallContext messageContext)
        {
            gateway.deviceManager.DeviceLeft(messageContext.callerNetworkDevice);
        }

        /// <summary>
        /// This method is responsible for informing the unknown equivalent driverss.
        /// </summary>
        /// <param name="serviceCall"></param>
        /// <param name="serviceResponse"></param>
        /// <param name="messageContext"></param>
        public void TellEquivalentDrivers(Call serviceCall, Response serviceResponse, CallContext messageContext)
        {
            try
            {
                string equivalentDrivers = serviceCall.GetParameterString(DRIVERS_NAME_KEY);
                IList<object> equivalentDriversJson = Json.Deserialize(equivalentDrivers) as IList<object>;
                List<object> jsonList = new List<object>();
                IDictionary<string, object> responseData = new Dictionary<string, object>();

                for (int i = 0; i < equivalentDriversJson.Count; i++)
                {
                    string equivalentDriver = equivalentDriversJson[i] as string;
                    UpDriver driver = gateway.driverManager.GetDriverFromEquivalanceTree(equivalentDriver);

                    if (driver != null)
                        AddToEquivalanceList(jsonList, driver);
                }

                responseData[INTERFACES_KEY] = Json.Serialize(jsonList);
                serviceResponse.responseData = responseData;
            }
            catch (System.Exception e)
            {
                logger.LogError("Problems on equivalent drivers. " + e.StackTrace);
            }
        }

        private void AddToEquivalanceList(IList<object> jsonList, UpDriver upDriver)
        {

            IList<string> equivalentDrivers = upDriver.equivalentDrivers;

            if (equivalentDrivers != null)
            {
                foreach (string equivalentDriver in equivalentDrivers)
                {
                    UpDriver driver = gateway.driverManager.GetDriverFromEquivalanceTree(equivalentDriver);
                    if (driver != null)
                        AddToEquivalanceList(jsonList, driver);
                }
            }

            jsonList.Add(upDriver.ToJSON());
        }
    }
}
