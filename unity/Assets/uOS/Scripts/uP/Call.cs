using MiniJSON;
using System.Collections.Generic;
using System.IO;


namespace UOS
{
    public class CallContext
    {
        private object _lock = new object();

        private List<ClientConnection> connections;

        public NetworkDevice callerNetworkDevice { get; set; }
        public UpDevice callerDevice { get; set; }
        public ClientConnection connection { get; private set; }


        public CallContext()
        {
            connections = new List<ClientConnection>();
        }

        public ClientConnection GetConnection()
        {
            return GetConnection(0);
        }

        public ClientConnection GetConnection(int index)
        {
            if (index < connections.Count)
                return connections[index];

            return null;
        }

        public void AddConnection(ClientConnection connection)
        {
            lock (_lock)
            {
                if (connection == null)
                    throw new System.ArgumentException("Connection can not be null");

                this.connections.Add(connection);
            }
        }
    }

    public enum ServiceType
    {
        DISCRETE,
        STREAM
    }

    public class Call : Message
    {
        public string driver { get; set; }
        public string service { get; set; }
        public IDictionary<string, object> parameters { get; set; }
        public string instanceId { get; set; }
        public ServiceType serviceType { get; set; }
        public int channels { get; set; }
        public string[] channelIDs { get; set; }
        public string channelType { get; set; }
        public string securityType { get; set; }

        public Call()
        {
            type = Message.Type.SERVICE_CALL_REQUEST;
            serviceType = ServiceType.DISCRETE;
        }

        public Call(string driver, string service)
            : this()
        {
            this.driver = driver;
            this.service = service;
        }

        public Call(string driver, string service, string instanceId)
            : this(driver, service)
        {
            this.instanceId = instanceId;
        }

        public Call AddParameter(string key, object value)
        {
            if (parameters == null)
                parameters = new Dictionary<string, object>();
            parameters[key] = value;

            return this;
        }

        public object GetParameter(string key)
        {
            object param = null;
            if ((parameters != null) && (parameters.TryGetValue(key, out param)))
                return param;

            return null;
        }

        public string GetParameterString(string key)
        {
            return GetParameter(key) as string;
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || (!(obj is Call)))
                return false;

            Call temp = (Call)obj;

            if (!Util.Compare(this.driver, temp.driver)) return false;
            if (!Util.Compare(this.service, temp.service)) return false;
            if (!Util.Compare(this.parameters, temp.parameters)) return false;
            if (!Util.Compare(this.instanceId, temp.instanceId)) return false;
            if (!Util.Compare(this.serviceType, temp.serviceType)) return false;
            if (!Util.Compare(this.channels, temp.channels)) return false;
            if (!Util.Compare(this.channelIDs, temp.channelIDs)) return false;
            if (!Util.Compare(this.channelType, temp.channelType)) return false;
            if (!Util.Compare(this.securityType, temp.securityType)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            if ((driver != null) && (service != null))
                return driver.GetHashCode() ^ service.GetHashCode();

            return base.GetHashCode();
        }

        public override object ToJSON()
        {
            IDictionary<string, object> json = base.ToJSON() as IDictionary<string, object>;

            Util.JsonPut(json, "driver", driver);
            Util.JsonPut(json, "service", service);
            Util.JsonPut(json, "parameters", parameters);
            Util.JsonPut(json, "instanceId", instanceId);
            Util.JsonPut(json, "serviceType", serviceType.ToString());
            Util.JsonPut(json, "channels", channels);
            Util.JsonPut(json, "channelIDs", channelIDs);
            Util.JsonPut(json, "channelType", channelType);
            Util.JsonPut(json, "securityType", securityType);

            return json;
        }

        public static Call FromJSON(object jsonObj)
        {
            Call call = new Call();
            Message.FromJSON(call, jsonObj);

            IDictionary<string, object> json = jsonObj as IDictionary<string, object>;

            call.driver = Util.JsonOptString(json, "driver");
            call.service = Util.JsonOptString(json, "service");
            call.parameters = Util.JsonOptField(json, "parameters") as IDictionary<string, object>;
            call.instanceId = Util.JsonOptString(json, "instanceId");
            call.serviceType = Util.JsonOptEnum<ServiceType>(json, "serviceType", call.serviceType);
            call.channels = Util.JsonOptInt(json, "channels", call.channels);

            List<string> ids = Util.JsonOptField(json, "channelIDs") as List<string>;
            if (ids != null)
            {
                call.channelIDs = new string[ids.Count];
                ids.CopyTo(call.channelIDs, 0);
            }

            call.channelType = Util.JsonOptString(json, "channelType");
            call.securityType = Util.JsonOptString(json, "securityType");

            return call;
        }

        public override string ToString()
        {
            return Json.Serialize(ToJSON());
        }
    }
}
