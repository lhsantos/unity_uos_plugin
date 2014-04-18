using System.Collections.Generic;


namespace UOS
{
    public class UpNetworkInterface
    {
        public string netType { get; set; }

        public string networkAddress { get; set; }

        public UpNetworkInterface()
            : this(null, null) { }

        public UpNetworkInterface(string netType, string networkAddress)
        {
            this.netType = netType;
            this.networkAddress = networkAddress;
        }

        public override bool Equals(object obj)
        {

            if ((obj == null) || (!(obj is UpNetworkInterface)))
                return false;

            UpNetworkInterface d = (UpNetworkInterface)obj;

            return
                Util.Compare(this.networkAddress, d.networkAddress) &&
                Util.Compare(this.netType, d.netType);
        }

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            if (netType != null)
                hash ^= netType.GetHashCode();
            if (networkAddress != null)
                hash ^= networkAddress.GetHashCode();

            return hash;
        }


        public object ToJSON()
        {
            IDictionary<string, object> ni_json = new Dictionary<string, object>();

            ni_json["networkAddress"] = networkAddress;
            ni_json["netType"] = netType;

            return ni_json;
        }

        public static UpNetworkInterface FromJSON(object json)
        {
            IDictionary<string, object> dict = json as IDictionary<string, object>;
            UpNetworkInterface ni = new UpNetworkInterface();

            ni.networkAddress = dict["networkAddress"] as string;
            ni.netType = dict["netType"] as string;

            return ni;
        }
    }
}
