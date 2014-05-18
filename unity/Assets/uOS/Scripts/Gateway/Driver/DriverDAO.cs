using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace UOS
{
    public class DriverDAO
    {
        private Dictionary<string, UpDriver> driverMap;
        private Dictionary<string, int> driverCount;
        private Dictionary<string, List<DriverModel>> driverByDeviceMap;
        private Dictionary<string, List<DriverModel>> driverByTypeMap;
        private Dictionary<long?, DriverModel> modelMap;
        private Dictionary<string, DriverModel> modelByIdMap;

        private static long rowid = 0;

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static long? NewId() { return rowid++; }


        public DriverDAO()
        {
            CreateMaps();
        }

        private void CreateMaps()
        {
            driverMap = new Dictionary<string, UpDriver>();
            driverCount = new Dictionary<string, int>();
            driverByDeviceMap = new Dictionary<string, List<DriverModel>>();
            modelMap = new Dictionary<long?, DriverModel>();
            driverByTypeMap = new Dictionary<string, List<DriverModel>>();
            modelByIdMap = new Dictionary<string, DriverModel>();
        }


        public void Insert(DriverModel model)
        {
            DriverModel found = Retrieve(model.id, model.device);
            if (found != null)
                RemoveFromMap(found);

            model.rowid = NewId();
            InsertOnMap(model);
        }

        public List<DriverModel> List()
        {
            return List(null, null);
        }

        public List<DriverModel> List(string name)
        {
            return List(name, null);
        }

        public List<DriverModel> List(string name, string device)
        {
            List<DriverModel> result = null;
            if (device == null)
            {
                if (name == null)
                    result = ListAll();
                else
                {
                    List<DriverModel> byDriver = ListByDriver(name);
                    result = byDriver;
                }
            }
            else
            {
                List<DriverModel> byDevice = ListByDevice(device);
                result = (name != null) ? ListByDeviceAndDriver(name, byDevice) : byDevice;
            }

            ((List<DriverModel>)result).Sort((m1, m2) => m1.id.CompareTo(m2.id));
            return new List<DriverModel>(result);
        }

        private List<DriverModel> ListAll()
        {
            return new List<DriverModel>(modelMap.Values);
        }

        private List<DriverModel> ListByDriver(string name)
        {
            List<DriverModel> result = null;
            if (driverByTypeMap.TryGetValue(name.ToLower(), out result))
                return result;

            return new List<DriverModel>();
        }

        private List<DriverModel> ListByDevice(string device)
        {
            List<DriverModel> result = null;
            if (driverByDeviceMap.TryGetValue(device.ToLower(), out result))
                return result;

            return new List<DriverModel>();
        }

        private List<DriverModel> ListByDeviceAndDriver(string name, List<DriverModel> listByDevice)
        {
            List<DriverModel> result = new List<DriverModel>();
            foreach (DriverModel d in listByDevice)
            {
                if (name.Equals(d.driver.name, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Add(d);
                }
            }

            return result;
        }

        public void Clear()
        {
            CreateMaps();
        }

        public void Delete(string id, string device)
        {
            DriverModel driver = Retrieve(id, device);
            RemoveFromMap(driver);
        }


        private void RemoveFromMap(DriverModel model)
        {
            modelMap.Remove(model.rowid);
            modelByIdMap.Remove(model.id);

            string name = model.driver.name.ToLower();
            int count = 0;
            if (driverCount.TryGetValue(name, out count))
            {
                count--;
                if (count == 0)
                {
                    driverCount.Remove(name);
                    driverMap.Remove(name);
                }
                else
                    driverCount[name] = count;
            }
            driverByTypeMap[name].Remove(model);

            string deviceName = model.device.ToLower();
            driverByDeviceMap[deviceName].Remove(model);
        }

        private void InsertOnMap(DriverModel model)
        {
            modelMap[model.rowid] = model;
            modelByIdMap[model.id] = model;

            string name = model.driver.name.ToLower();
            if (!driverMap.ContainsKey(name))
            {
                driverMap[name] = model.driver;
                driverCount[name] = 0;
                driverByTypeMap[name] = new List<DriverModel>();
            }
            driverCount[name]++;
            driverByTypeMap[name].Add(model);

            string deviceName = model.device.ToLower();
            if (!driverByDeviceMap.ContainsKey(deviceName))
                driverByDeviceMap[deviceName] = new List<DriverModel>();
            driverByDeviceMap[deviceName].Add(model);
        }

        public DriverModel Retrieve(string id, string device)
        {
            // find by id
            if ((id != null) && (device == null))
                return modelByIdMap[id];
            else if (device != null)
            {
                List<DriverModel> drivers;
                if (driverByDeviceMap.TryGetValue(device.ToLower(), out drivers) && (drivers.Count > 0))
                {
                    // find by driver
                    if (id == null)
                        return drivers[0];

                    // find by driver and id
                    return drivers.Find(d => d.id == id);
                }
            }

            return null;
        }
    }
}
