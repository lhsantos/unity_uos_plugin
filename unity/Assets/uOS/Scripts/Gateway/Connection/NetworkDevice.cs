namespace UOS
{
    /// <summary>
    /// A generic device found on a network.
    /// </summary>
    public abstract class NetworkDevice
    {
        public abstract string networkDeviceName { get; }
        public abstract string networkDeviceType { get; }
    }
}
