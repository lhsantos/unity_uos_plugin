using MiniJSON;
using System.Collections.Generic;


namespace UOS
{
    public class UpDevice
    {
        public string name { get; set; }
        public List<UpNetworkInterface> networks { get; set; }
        public IDictionary<string, string> meta { get; private set; }


        public UpDevice(string name = null)
        {
            this.name = name;
        }

        public UpDevice AddNetworkInterface(string networkAdress, string networkType)
        {
            if (networks == null)
                networks = new List<UpNetworkInterface>();

            networks.Add(new UpNetworkInterface(networkType, networkAdress));

            return this;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is UpDevice))
                return false;

            UpDevice d = (UpDevice)obj;

            return
                Util.Compare(this.name, d.name) &&
                Util.Compare(this.networks, d.networks) &&
                Util.Compare(this.meta, d.meta);
        }

        public override int GetHashCode()
        {
            if (name != null)
                return name.GetHashCode();

            return base.GetHashCode();
        }

        public override string ToString()
        {
            //TODO: MiniJSON does not throws exceptions, should we deal with it?
            return Json.Serialize(ToJSON());
        }

        public object GetProperty(string key)
        {
            if (meta == null)
                return null;

            return meta[key];
        }

        public void AddProperty(string key, string value)
        {
            if (meta == null)
                meta = new Dictionary<string, string>();

            meta[key] = value;
        }

        public object ToJSON()
        {
            IDictionary<string, object> json = new Dictionary<string, object>();

            Util.JsonPut(json, "name", name);

            if (networks != null)
            {
                List<object> j_networks = new List<object>();
                foreach (var ni in networks)
                    j_networks.Add(ni.ToJSON());
                Util.JsonPut(json, "networks", j_networks);
            }

            if (meta != null)
                Util.JsonPut(json, "meta", meta);

            return json;
        }

        public static UpDevice FromJSON(object json)
        {
            IDictionary<string, object> dict = json as IDictionary<string, object>;

            UpDevice device = new UpDevice();
            device.name = Util.JsonOptString(dict, "name");
            device.networks = FromNetworks(dict);
            device.meta = FromMeta(dict);

            return device;
        }

        private static IDictionary<string, string> FromMeta(IDictionary<string, object> json)
        {
            object meta = Util.JsonOptField(json, "meta");
            if (meta != null)
            {
                IDictionary<string, string> dest = new Dictionary<string, string>();
                foreach (var pair in meta as IDictionary<string, object>)
                    dest[pair.Key] = pair.Value as string;
                return dest;
            }

            return null;
        }

        private static List<UpNetworkInterface> FromNetworks(IDictionary<string, object> json)
        {
            object obj = Util.JsonOptField(json, "networks");
            if (obj != null)
            {
                List<object> j_networks = obj as List<object>;
                List<UpNetworkInterface> networks = new List<UpNetworkInterface>();
                foreach (var j_network in j_networks)
                    networks.Add(UpNetworkInterface.FromJSON(j_network));

                return networks;
            }

            return null;
        }
    }
}
