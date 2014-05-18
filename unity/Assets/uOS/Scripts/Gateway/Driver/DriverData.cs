namespace UOS
{
    public class DriverData
    {
        public string instanceID { get; private set; }
        public UpDriver driver { get; private set; }
        public UpDevice device { get; private set; }

        public DriverData(UpDriver driver, UpDevice device, string instanceID)
        {
            this.driver = driver;
            this.device = device;
            this.instanceID = instanceID;
        }

        public override bool Equals(object obj)
        {

            if ((obj != null) && (obj is DriverData))
            {
                DriverData temp = (DriverData)obj;
                if (temp.driver != null &&
                        temp.device != null &&
                        temp.instanceID != null)
                {

                    return temp.driver.Equals(this.driver) &&
                            temp.device.Equals(this.device) &&
                            temp.instanceID.Equals(this.instanceID);
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            if ((instanceID != null) && (driver != null) && (device != null))
                return instanceID.GetHashCode() ^ driver.GetHashCode() ^ device.GetHashCode();

            return base.GetHashCode();
        }
    }
}
