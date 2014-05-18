using MiniJSON;
using System.Collections.Generic;


namespace UOS
{
    public class UpDriver
    {
        public string name { get; set; }
        public List<UpService> services { get; set; }
        public List<UpService> events { get; set; }
        public List<string> equivalentDrivers { get; set; }

        public UpDriver() { }

        public UpDriver(string name)
        {
            this.name = name;
        }

        public UpDriver AddEvent(UpService evt)
        {
            if (events == null)
                events = new List<UpService>();

            events.Add(evt);

            return this;
        }

        public UpService AddEvent(string evt)
        {
            UpService sEvent = new UpService(evt);

            AddEvent(sEvent);

            return sEvent;
        }

        public UpService AddService(UpService service)
        {
            if (services == null)
                services = new List<UpService>();

            services.Add(service);

            return service;
        }

        public UpService AddService(string serviceName)
        {
            return AddService(new UpService(serviceName));
        }

        public List<string> AddEquivalentDrivers(string driver)
        {
            if (equivalentDrivers == null)
                equivalentDrivers = new List<string>();

            equivalentDrivers.Add(driver);

            return equivalentDrivers;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is UpDriver))
                return false;

            UpDriver d = (UpDriver)obj;

            return
                Util.Compare(this.name, d.name) &&
                Util.Compare(this.services, d.services) &&
                Util.Compare(this.events, d.events);
        }

        public override int GetHashCode()
        {
            if (name != null)
                return name.GetHashCode();

            return base.GetHashCode();
        }

        public object ToJSON()
        {
            IDictionary<string, object> json = new Dictionary<string, object>();

            Util.JsonPut(json, "name", name);

            AddServices(json, "services", this.services);
            AddServices(json, "events", this.events);
            AddStrings(json, "equivalent_drivers", equivalentDrivers);

            return json;
        }

        private void AddStrings(IDictionary<string, object> json, string propName, List<string> stringList)
        {
            if (stringList != null)
            {
                List<string> json_array = new List<string>(stringList);
                json[propName] = json_array;
            }
        }

        private void AddServices(IDictionary<string, object> json, string propName, List<UpService> serviceList)
        {
            if (serviceList != null)
            {
                List<object> json_array = new List<object>();
                foreach (UpService s in serviceList)
                    json_array.Add(s.ToJSON());
                json[propName] = json_array;
            }
        }

        public static UpDriver FromJSON(object obj)
        {
            IDictionary<string, object> json = obj as IDictionary<string, object>;
            UpDriver d = new UpDriver(Util.JsonOptString(json, "name"));

            d.services = ServicesFromJSON(json, "services");
            d.events = ServicesFromJSON(json, "events");
            d.equivalentDrivers = StringsFromJSON(json, "equivalent_drivers");

            return d;
        }

        private static List<string> StringsFromJSON(IDictionary<string, object> json, string propName)
        {
            List<object> array = Util.JsonOptField(json, propName) as List<object>;
            if (array != null)
            {
                List<string> strings = new List<string>();
                foreach (var o in array)
                    strings.Add(o as string);

                return strings;
            }

            return null;
        }

        private static List<UpService> ServicesFromJSON(IDictionary<string, object> json, string propName)
        {
            List<object> jsonArray = Util.JsonOptField(json, propName) as List<object>;
            if (jsonArray != null)
            {
                List<UpService> services = new List<UpService>();
                foreach (var o in jsonArray)
                    services.Add(UpService.FromJSON(o));

                return services;
            }

            return null;
        }
    }
}
