using MiniJSON;
using System.Collections.Generic;


namespace UOS
{
    public class DeviceDriver : UOSDriver
    {
        private const string DEVICE_KEY = "device";
        private const string SECURITY_TYPE_KEY = "securityType";
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

            //driver.AddService("tellEquivalentDriver")
            //    .AddParameter(DRIVER_NAME_KEY, UpService.ParameterType.MANDATORY);
        }

        public UpDriver GetDriver()
        {
            return driver;
        }

        public IList<UpDriver> GetParent()
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
                        gateway.GetCurrentDevice().name
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
                gateway.RegisterDevice(device);

                serviceResponse.AddParameter(DEVICE_KEY, Json.Serialize(gateway.GetCurrentDevice().ToJSON()));

                //TODO: actually implement the driver register for other devices...
                //gateway.CallService(
                //    device,
                //    new Call("uos.DeviceDriver", "listDrivers"),
                //    new uOSServiceCallBack(
                //        delegate(uOSServiceCallInfo info, Response r, System.Exception e)
                //        {
                //            if ((e != null) || (r == null))
                //            {
                //                logger.LogError(
                //                    "Problems on listing drivers for handshake: " +
                //                    ((e != null) ? (e.Message + "," + e.StackTrace) : "no response"));
                //            }
                //            else
                //            {
                //                object driverList = r.GetResponseData("driverList");
                //                if (driverList != null)
                //                {
                //                    var driverMap = Json.Deserialize(driverList as string) as IDictionary<string, object>;
                //                    // TODO: this is duplicated with DeviceManager.registerRemoteDriverInstances
                //                    foreach (string id in driverMap.Keys)
                //                    {
                //                        UpDriver upDriver = UpDriver.FromJSON(Json.Deserialize(driverMap[id] as string));
                //                        gateway.driverManager.Register(id, upDriver, device.name);
                //                    }
                //                }
                //            }
                //        }
                //    )
                //);
            }
            catch (System.Exception e)
            {
                serviceResponse.error = e.Message;
                logger.LogError("Problems on handshake: " + e.Message + "," + e.StackTrace);
            }
        }

        public void Goodbye(Call serviceCall, Response serviceResponse, CallContext messageContext)
        {
            gateway.DeviceLeft(messageContext.callerNetworkDevice);
        }
    }
}
