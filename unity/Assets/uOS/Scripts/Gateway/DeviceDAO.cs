using System.Collections.Generic;


namespace UOS
{
    public class DeviceDAO
    {
        private IDictionary<string, UpDevice> deviceMap;
        private IDictionary<string, UpDevice> interfaceMap;
        private IDictionary<string, List<UpDevice>> networkTypeMap;
        private IDictionary<string, List<UpDevice>> addressMap;

        public DeviceDAO()
        {
            deviceMap = new Dictionary<string, UpDevice>();
            interfaceMap = new Dictionary<string, UpDevice>();
            networkTypeMap = new Dictionary<string, List<UpDevice>>();
            addressMap = new Dictionary<string, List<UpDevice>>();
        }

        public void Add(UpDevice device)
        {
            if (device.networks != null)
            {
                foreach (UpNetworkInterface ni in device.networks)
                {
                    interfaceMap[GenerateInterfaceKey(ni)] = device;

                    List<UpDevice> devices = null;
                    if (!networkTypeMap.TryGetValue(ni.netType, out devices))
                    {
                        devices = new List<UpDevice>();
                        networkTypeMap[ni.netType] = devices;
                    }
                    devices.Add(device);

                    if (!addressMap.TryGetValue(ni.networkAddress, out devices))
                    {
                        devices = new List<UpDevice>();
                        addressMap[ni.networkAddress] = devices;
                    }
                    devices.Add(device);
                }
            }

            deviceMap[device.name.ToLower()] = device;
        }

        public void Update(string oldname, UpDevice device)
        {
            Delete(oldname);
            Add(device);
        }

        public void Delete(string name)
        {
            UpDevice device = Find(name);
            if (device.networks != null)
            {
                foreach (UpNetworkInterface ni in device.networks)
                {
                    interfaceMap.Remove(GenerateInterfaceKey(ni));
                    networkTypeMap[ni.netType].Remove(device);
                    addressMap[ni.networkAddress].Remove(device);
                }
            }
            deviceMap.Remove(name.ToLower());
        }

        public List<UpDevice> List()
        {
            return new List<UpDevice>(deviceMap.Values);
        }

        public List<UpDevice> List(string address, string networktype)
        {
            if ((address != null) && (networktype != null))
            {
                List<UpDevice> ret = new List<UpDevice>();

                string key = GenerateInterfaceKey(new UpNetworkInterface(networktype, address));
                UpDevice upDevice = null;
                if (interfaceMap.TryGetValue(key, out upDevice))
                    ret.Add(upDevice);

                return ret;
            }
            else if (address != null)
            {
                List<UpDevice> devices = null;
                if (addressMap.TryGetValue(address, out devices))
                    return new List<UpDevice>(new HashSet<UpDevice>(devices));
            }
            else if (networktype != null)
            {
                List<UpDevice> devices = null;
                if (networkTypeMap.TryGetValue(networktype, out devices))
                    return new List<UpDevice>(new HashSet<UpDevice>(devices));
            }
            else
                return List();

            return new List<UpDevice>();
        }

        public UpDevice Find(string name)
        {
            UpDevice device = null;
            if (deviceMap.TryGetValue(name.ToLower(), out device))
                return device;

            return null;
        }

        public void Clear()
        {
            deviceMap.Clear();
            interfaceMap.Clear();
            addressMap.Clear();
            networkTypeMap.Clear();
        }



        private static string GenerateInterfaceKey(UpNetworkInterface ni)
        {
            return ni.networkAddress + "@" + ni.netType;
        }
    }
}
