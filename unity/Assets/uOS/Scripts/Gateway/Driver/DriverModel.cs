namespace UOS
{
    public class DriverModel
    {
        public const string TABLE = "DRIVER";
        public const string ROW_ID = "row_id";
        public const string ID = "id";
        public const string NAME = "name";
        public const string DEVICE = "device";


        public long? rowid { get; set; }
        public string id { get; private set; }
        public UpDriver driver { get; private set; }
        public string device { get; private set; }

        public DriverModel(string id, UpDriver driver, string device)
            : this(null, id, driver, device) { }

        public DriverModel(long? rowid, string id, UpDriver driver, string device)
        {
            this.rowid = rowid;
            this.id = id;
            this.driver = driver;
            this.device = device;
        }

        public override int GetHashCode()
        {
            int code = 0;

            if (driver != null)
                code ^= driver.GetHashCode();
            if (device != null)
                code ^= device.GetHashCode();
            if (id != null)
                code ^= id.GetHashCode();

            return (code != 0) ? code : base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !(obj is DriverModel))
                return false;

            DriverModel d = (DriverModel)obj;

            return
                driver.Equals(d.driver) &&
                device.Equals(d.device) &&
                id.Equals(d.id);
        }
    }
}
