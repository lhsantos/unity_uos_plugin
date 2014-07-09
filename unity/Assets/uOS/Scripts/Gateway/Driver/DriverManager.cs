using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace UOS
{
    public class DriverManager
    {
        private uOSSettings settings;
        private UnityGateway gateway;
        private Logger logger = null;
        private object _driverdao_lock = new object();

        private UpDevice currentDevice;
        private IDictionary<long?, UOSDriver> instances;
        private List<string> toInitialize;
        private IDictionary<string, TreeNode> driverHash;
        private List<TreeNode> tree;

        public DriverDAO driverDao { get; private set; }


        public DriverManager(
                uOSSettings settings,
                UnityGateway gateway,
                UpDevice currentDevice)
        {
            this.settings = settings;
            this.gateway = gateway;
            this.logger = gateway.logger;
            this.driverDao = new DriverDAO();
            this.currentDevice = currentDevice;

            this.instances = new Dictionary<long?, UOSDriver>();
            this.toInitialize = new List<string>();
            this.driverHash = new Dictionary<string, TreeNode>();

            InitTree();
        }

        /// <summary>
        /// Initialises driver tree.
        /// </summary>
        private void InitTree()
        {
            tree = new List<TreeNode>();

            //TreeNode pointer = new TreeNode(DefaultDrivers.POINTER.getDriver());
            //tree.add(pointer);
            //driverHash.put(Pointer.DRIVER_NAME, pointer);
        }

        /// <summary>
        /// Finds an equivalent driver to the listed drivers.
        /// </summary>
        /// <param name="equivalentDrivers"></param>
        /// <returns></returns>
        private List<DriverModel> FindEquivalentDriver(List<TreeNode> equivalentDrivers)
        {
            List<DriverModel> list;

            foreach (TreeNode treeNode in equivalentDrivers)
            {
                lock (_driverdao_lock) { list = driverDao.List(treeNode.driver.name, currentDevice.name); }

                if ((list != null) && (list.Count > 0))
                    return list;
            }

            foreach (TreeNode treeNode in equivalentDrivers)
            {
                list = FindEquivalentDriver(treeNode.children);
                if ((list != null) && (list.Count > 0))
                    return list;
            }

            return null;
        }

        public Response HandleServiceCall(Call serviceCall, CallContext messageContext)
        {
            //Handle named InstanceCall
            DriverModel model = null;
            if (serviceCall.instanceId != null)
            {
                //Find DriversInstance
                lock (_driverdao_lock) { model = driverDao.Retrieve(serviceCall.instanceId, currentDevice.name); }
                if (model == null)
                {
                    logger.LogError("No Instance found with id '" + serviceCall.instanceId + "'");
                    throw new System.Exception("No Instance found with id '" + serviceCall.instanceId + "'");
                }
            }
            else
            {
                //Handle non-named InstanceCall
                List<DriverModel> list;
                lock (_driverdao_lock) { list = driverDao.List(serviceCall.driver, currentDevice.name); }
                //Tries to find an equivalent driver...
                if ((list == null) || (list.Count == 0))
                {
                    TreeNode driverNode = null;
                    if (!driverHash.TryGetValue(serviceCall.driver, out driverNode))
                    {
                        logger.Log("No instance found for handling driver '" + serviceCall.driver + "'");
                        throw new System.Exception("No instance found for handling driver '" + serviceCall.driver + "'");
                    }

                    list = FindEquivalentDriver(driverNode.children);
                    if ((list == null) || (list.Count == 0))
                    {
                        logger.Log("No instance found for handling driver '" + serviceCall.driver + "'");
                        throw new System.Exception("No instance found for handling driver '" + serviceCall.driver + "'");
                    }
                }

                // Select the first driver found (since no specific instance was informed)
                model = list[0];
            }

            return gateway.reflectionServiceCaller.CallService(instances[model.rowid], serviceCall, messageContext);
        }

        /// <summary>
        /// Deploys a driver into the context.
        /// </summary>
        /// <param name="instance">Instance of the object implementing the informed Driver.</param>
        /// <param name="instanceId">Optional instanceId which to call this instance of the driver.</param>
        public void DeployDriver(UOSDriver instance, string instanceId = null)
        {
            UpDriver driver = instance.GetDriver();

            if (instanceId == null)
                instanceId = driver.name + IncDeployedDriversCount();

            DriverModel model = new DriverModel(instanceId, instance.GetDriver(), this.currentDevice.name);

            if (!driverHash.ContainsKey(driver.name))
            {
                if (instance.GetParent() != null)
                    AddToEquivalenceTree(instance.GetParent());
                AddToEquivalenceTree(driver);
            }

            lock (_driverdao_lock) { driverDao.Insert(model); }
            instances[model.rowid] = instance;
            toInitialize.Add(instanceId);
            logger.Log("Deployed Driver : " + model.driver.name + " with id " + instanceId);
        }

        static long deployCount = 0;
        [MethodImpl(MethodImplOptions.Synchronized)]
        private long IncDeployedDriversCount() { return ++deployCount; }


        /// <summary>
        /// Adds a list of drivers to the driverHash and to the equivalence tree.
        /// </summary>
        /// <param name="drivers">Objects representing the interfaces of the Drivers to be added</param>
        public void AddToEquivalenceTree(List<UpDriver> drivers)
        {
            int removeTries = 0;

            while (drivers.Count > 0)
            {
                UpDriver driver = drivers[0];
                try
                {
                    AddToEquivalenceTree(driver);
                    drivers.Remove(driver);
                    removeTries = 0;
                }
                catch (DriverNotFoundException)
                {
                    drivers.Remove(driver);
                    drivers.Add(driver);
                    removeTries++;
                }

                if ((removeTries == drivers.Count) && (removeTries != 0))
                    throw new InterfaceValidationException("The driver did not informe the complete list of equivalent drivers.");
            }
        }

        /// <summary>
        /// Adds the driver to the driverHash and to the equivalence tree.
        /// </summary>
        /// <param name="driver">Object representing the interface of the Driver to be added</param>
        public void AddToEquivalenceTree(UpDriver driver)
        {
            TreeNode node = new TreeNode(driver);
            List<string> equivalentDrivers = driver.equivalentDrivers;
            HashSet<string> driversNotFound = new HashSet<string>();

            if (equivalentDrivers != null)
            {
                foreach (string equivalentDriver in equivalentDrivers)
                {
                    TreeNode parent = null;
                    if (!driverHash.TryGetValue(equivalentDriver, out parent))
                        driversNotFound.Add(equivalentDriver);
                    else
                    {
                        ValidateInterfaces(parent.driver.services, node.driver.services);
                        ValidateInterfaces(parent.driver.events, node.driver.events);
                        parent.AddChild(node);
                    }
                }
            }
            else
                tree.Add(node);

            if (driversNotFound.Count > 0)
                throw new DriverNotFoundException("Equivalent drivers not found.", driversNotFound);

            driverHash[driver.name] = node;
        }

        private void ValidateInterfaces(List<UpService> parentServices, List<UpService> driverServices)
        {
            if ((parentServices == null) && (driverServices == null))
                return;

            if ((parentServices != null && driverServices != null) || (parentServices != null && driverServices == null))
                throw new InterfaceValidationException("The deployed DriverInstance must have the same parameters.");

            foreach (UpService parentService in parentServices)
            {
                var driverService = driverServices.Find(s => s.Equals(parentService));
                if (driverService == null)
                    throw new InterfaceValidationException("The deployed DriverInstance must have the same service name than its parent.");

                var parameters = parentService.parameters;
                if (parameters != null)
                {
                    var driverParameters = driverService.parameters;
                    if ((driverParameters != null) && (parameters.Count == driverParameters.Count))
                    {
                        foreach (var parameter in parameters)
                        {
                            UpService.ParameterType? driverParameter = null;
                            if (!(driverParameters.TryGetValue(parameter.Key, out driverParameter)
                                  && parameter.Value.Equals(driverParameter)))
                                throw new InterfaceValidationException("The deployed DriverInstance must have the same parameter names and types.");
                        }
                    }
                    else
                        throw new InterfaceValidationException("The deployed DriverInstance must have the same parameter quantities.");
                }
                else if (driverService.parameters != null)
                    throw new InterfaceValidationException("The deployed DriverInstance must have the same parameter quantities.");
            }
        }

        /// <summary>
        /// Undeploys the referenced driver instance from the Driver.
        /// </summary>
        /// <param name="instanceId">The instance id of the Driver to be removed.</param>
        public void UndeployDriver(string instanceId)
        {
            logger.Log("Undeploying driver with InstanceId : '" + instanceId + "'");

            DriverModel model;
            lock (_driverdao_lock) { model = driverDao.Retrieve(instanceId, currentDevice.name); }

            if (model != null)
            {
                UOSDriver uDriver = instances[model.rowid];
                if (!toInitialize.Contains(model.id))
                    uDriver.Destroy();

                lock (_driverdao_lock) { driverDao.Delete(model.id, currentDevice.name); }
                toInitialize.Remove(model.id);
            }
            else
                logger.LogError(
                    "Undeploying driver with InstanceId : '" + instanceId +
                    "' was not possible, since it's not present in the current database.");
        }

        /// <summary>
        /// Lists all deployed drivers.
        /// </summary>
        /// <returns>The drivers.</returns>
        public List<UOSDriver> ListDrivers()
        {
            List<DriverModel> list;
            lock (_driverdao_lock) { list = driverDao.List(null, currentDevice.name); }

            if (list.Count == 0)
                return null;

            List<UOSDriver> ret = new List<UOSDriver>();
            foreach (DriverModel m in list)
                ret.Add(instances[m.rowid]);

            return ret;
        }

        private List<DriverModel> FindAllEquivalentDrivers(List<TreeNode> equivalentDrivers)
        {
            List<DriverModel> list = new List<DriverModel>();

            if (equivalentDrivers == null || equivalentDrivers.Count == 0)
                return list;

            foreach (TreeNode treeNode in equivalentDrivers)
            {
                List<DriverModel> aux;
                lock (_driverdao_lock) { aux = driverDao.List(treeNode.driver.name, null); }
                list.AddRange(aux); //TODO: [B&M] Should we filter the device?!

                List<DriverModel> temp = FindAllEquivalentDrivers(treeNode.children);
                foreach (DriverModel driverModel in temp)
                    if (!list.Contains(driverModel))
                        list.Add(driverModel);
            }

            return list;
        }

        public UpDriver GetDriverFromEquivalanceTree(string driverName)
        {
            TreeNode driver = null;
            if (!driverHash.TryGetValue(driverName, out driver))
                return null;
            return driver.driver;
        }

        /// <summary>
        /// Lists known driver data about the environment.
        /// </summary>
        /// <param name="driverName">Driver name used to filter results.</param>
        /// <param name="deviceName">Device name used to filter results.</param>
        /// <returns></returns>
        public List<DriverData> ListDrivers(string driverName, string deviceName)
        {
            List<DriverModel> list;
            lock (_driverdao_lock) { list = driverDao.List(driverName, deviceName); }
            HashSet<DriverModel> baseSet = new HashSet<DriverModel>(list);

            TreeNode driverNode = null;
            if ((driverName != null) && driverHash.TryGetValue(driverName, out driverNode))
            {
                List<TreeNode> equivalentDrivers = driverNode.children;
                baseSet.UnionWith(FindAllEquivalentDrivers(equivalentDrivers));
            }
            if (baseSet == null || baseSet.Count == 0)
                return null;

            List<DriverData> ret = new List<DriverData>();
            foreach (DriverModel dm in baseSet)
                ret.Add(new DriverData(dm.driver, gateway.deviceManager.deviceDao.Find(dm.device), dm.id));

            return ret;
        }

        /// <summary>
        /// Initializes the driver that are not initialized yet.
        /// </summary>
        public void InitDrivers()
        {
            List<DriverModel> list;
            lock (_driverdao_lock) { list = driverDao.List("uos.DeviceDriver"); }
            if (list.Count == 0)
            {
                DeviceDriver deviceDriver = new DeviceDriver();
                DeployDriver(deviceDriver);
            }

            logger.Log("Initializing " + toInitialize.Count + " drivers.");
            foreach (string id in toInitialize)
            {
                DriverModel model;
                lock (_driverdao_lock) { model = driverDao.Retrieve(id, currentDevice.name); }
                UOSDriver driver = instances[model.rowid];
                driver.Init(gateway, settings, id);
                logger.Log("Initialized Driver " + model.driver.name + " with id '" + id + "'");
            }
            toInitialize.Clear();
        }

        /// <summary>
        /// Releases the resources allocated by the DriverManager and inform the deployed drivers of the
        /// shutdown of the application.
        /// </summary>
        public void TearDown()
        {
            List<DriverModel> list;
            lock (_driverdao_lock) { list = driverDao.List(); }
            foreach (DriverModel d in list)
                UndeployDriver(d.id);

            deployCount = 0;
        }

        public UOSDriver GetDriver(string id)
        {
            DriverModel model = driverDao.Retrieve(id, currentDevice.name);
            if (model != null)
                return instances[model.rowid];
            else
                return null;
        }

        public List<DriverModel> List(string name, string device)
        {
            lock (_driverdao_lock)
            {
                return driverDao.List(name, device);
            }
        }

        public void Delete(string id, string device)
        {
            lock (_driverdao_lock)
            {
                driverDao.Delete(id, device);
            }
        }

        public void Insert(DriverModel driverModel)
        {
            AddToEquivalenceTree(driverModel.driver);
            lock (_driverdao_lock)
            {
                driverDao.Insert(driverModel);
            }
        }
    }

    public class DriverNotFoundException : System.Exception
    {
        public HashSet<string> driversNames { get; private set; }

        public DriverNotFoundException(string message, HashSet<string> driversName)
            : base(message)
        {
            this.driversNames = driversName;
        }
    }

    public class InterfaceValidationException : System.Exception
    {
        public InterfaceValidationException(string msg)
            : base(msg) { }
    }
}
