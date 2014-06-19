using System.Collections.Generic;


namespace UOS
{
    public struct uOSServiceCallInfo
    {
        public UpDevice device;
        public Call call;
        public object asyncState;
    }

    public delegate void uOSServiceCallBack(uOSServiceCallInfo info, Response response, System.Exception e);


    public interface IGateway
    {
        /// <summary>
        /// Retrieves the uOS device this app is running on.
        /// </summary>
        /// <returns></returns>
        UpDevice currentDevice { get; }


        /// <summary>
        /// Initilises this Gateway.
        /// </summary>
        void Init();

        /// <summary>
        /// Releases all resources associated with this Gateway.
        /// </summary>
        void TearDown();

        //Response CallService(
        //    UpDevice device,
        //    string serviceName,
        //    string driverName,
        //    string instanceId,
        //    string securityType,
        //    IDictionary<string, object> parameters
        //);

        /// <summary>
        /// Synchronous service call. This method will block until the service response is ready.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceCall"></param>
        /// <returns></returns>
        Response CallService(
            UpDevice device,
            Call serviceCall
        );

        /// <summary>
        /// Asynchronous service call.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceCall"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        void CallServiceAsync(
            UpDevice device,
            Call serviceCall,
            uOSServiceCallBack callback,
            object state = null
        );

        //void Register(
        //    UosEventListener listener,
        //    UpDevice device,
        //    string driver,
        //    string eventKey
        //);

        //void Register(
        //    UosEventListener listener,
        //    UpDevice device,
        //    string driver,
        //    string instanceId,
        //    string eventKey
        //);

        //void Register(
        //    UosEventListener listener,
        //    UpDevice device,
        //    string driver,
        //    string instanceId,
        //    string eventKey,
        //    IDictionary<string, object> parameters
        //);

        //void Unregister(UosEventListener listener);

        //void Unregister(
        //    UosEventListener listener,
        //    UpDevice device,
        //    string driver,
        //    string instanceId,
        //    string eventKey
        //);

        //void DoNotify(
        //    Notify notify,
        //    UpDevice device
        //);

        /// <summary>
        /// Lists all drivers found so far.
        /// </summary>
        /// <param name="driverName"></param>
        /// <returns></returns>
        List<DriverData> ListDrivers(string driverName);

        /// <summary>
        /// Lists all found devices so far.
        /// </summary>
        /// <returns></returns>
        List<UpDevice> ListDevices();
    }

    public class ServiceCallException : System.Exception
    {
        public ServiceCallException() { }

        public ServiceCallException(string msg) : base(msg) { }

        public ServiceCallException(System.Exception e) : base(null, e) { }
    }
}
