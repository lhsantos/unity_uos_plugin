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

        /// <summary>
        /// Register a Listener for an event, driver and device specified.
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="device"></param>
        /// <param name="driver"></param>
        /// <param name="instanceId"></param>
        /// <param name="eventKey"></param>
        /// <param name="parameters"></param>
        void Register(
            UOSEventListener listener,
            UpDevice device,
            string driver,
            string instanceId = null,
            string eventKey = null,
            IDictionary<string, object> parameters = null
        );

        /// <summary>
        /// Removes a listener for receiving Notify events and notifies the event driver of its removal.
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="device"></param>
        /// <param name="driver"></param>
        /// <param name="instanceId"></param>
        /// <param name="eventKey"></param>
        void Unregister(
            UOSEventListener listener,
            UpDevice device = null,
            string driver = null,
            string instanceId = null,
            string eventKey = null
        );

        /// <summary>
        /// Sends a notify message to the device informed.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="notify"></param>
        void Notify(Notify notify, UpDevice device);

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
