namespace UOS
{
    public interface ChannelManager
    {
        string GetNetworkDeviceType();

        ClientConnection OpenPassiveConnection(string networkDeviceName);

        ClientConnection OpenActiveConnection(string networkDeviceName);

        NetworkDevice GetAvailableNetworkDevice();
    }
}
