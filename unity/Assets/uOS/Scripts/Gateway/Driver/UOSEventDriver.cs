namespace UOS
{
    public interface UOSEventDriver : UOSDriver
    {
        /// <summary>
        /// Service responsible for registering the caller device as a listener for the event under the key informed 
        /// in the 'eventKey' parameter for the implementing driver.
        /// </summary>
        void RegisterListener(Call serviceCall, Response serviceResponse, CallContext messageContext);

        /// <summary>
        /// Service responsible for removing the caller device as a listener for the event under the key informed 
        /// in the 'eventKey' parameter for the implementing driver. If no key is informed the current device will be
        /// removed as listener from all event queues.
        /// </summary>
        void UnregisterListener(Call serviceCall, Response serviceResponse, CallContext messageContext);
    }
}
