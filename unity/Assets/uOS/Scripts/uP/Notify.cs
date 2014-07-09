using MiniJSON;
using System.Collections.Generic;


namespace UOS
{
    public class Notify : Message
    {
        public string eventKey { get; set; }

        public string driver { get; set; }

        public string instanceId { get; set; }

        public IDictionary<string, object> parameters { get; set; }


        public Notify(string eventKey = null, string driver = null, string instanceId = null)
        {
            this.type = Message.Type.NOTIFY;
            this.eventKey = eventKey;
            this.driver = driver;
            this.instanceId = instanceId;
        }

        public Notify AddParameter(string key, string value)
        {
            return AddParameter(key, (object)value);
        }

        public Notify AddParameter(string key, object value)
        {
            if (parameters == null)
                parameters = new Dictionary<string, object>();

            parameters[key] = value;

            return this;
        }

        public object GetParameter(string key)
        {
            if (parameters != null)
            {
                object obj = null;
                if (parameters.TryGetValue(key, out obj))
                    return obj;
            }

            return null;
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || (!(obj is Notify)))
                return false;

            Notify temp = (Notify)obj;

            return
                Util.Compare(this.eventKey, temp.eventKey) &&
                Util.Compare(this.driver, temp.driver) &&
                Util.Compare(this.instanceId, temp.instanceId) &&
                Util.Compare(this.parameters, temp.parameters);
        }

        public override int GetHashCode()
        {
            int hash = 0;

            if (eventKey != null)
                hash ^= eventKey.GetHashCode();

            if (driver != null)
                hash ^= driver.GetHashCode();

            if (instanceId != null)
                hash ^= instanceId.GetHashCode();

            return hash;
        }

        public override object ToJSON()
        {
            IDictionary<string, object> json = base.ToJSON() as IDictionary<string, object>;

            Util.JsonPut(json, "eventKey", eventKey);
            Util.JsonPut(json, "driver", driver);
            Util.JsonPut(json, "instanceId", instanceId);
            Util.JsonPut(json, "parameters", parameters);

            return json;
        }

        public static Notify FromJSON(object jsonObj)
        {
            Notify e = new Notify();
            Message.FromJSON(e, jsonObj);

            IDictionary<string, object> json = jsonObj as IDictionary<string, object>;

            e.eventKey = Util.JsonOptString(json, "eventKey");
            e.driver = Util.JsonOptString(json, "driver");
            e.instanceId = Util.JsonOptString(json, "instanceId");
            e.parameters = Util.JsonOptField(json, "parameters") as IDictionary<string, object>;

            return e;
        }

        public override string ToString()
        {
            return Json.Serialize(ToJSON());
        }
    }
}
