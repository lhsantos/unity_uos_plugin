using MiniJSON;
using System.Collections.Generic;


namespace UOS
{
    public class Message
    {
        public enum Type
        {
            SERVICE_CALL_REQUEST,
            SERVICE_CALL_RESPONSE,
            NOTIFY,
            ENCAPSULATED_MESSAGE
        }

        public Type type { get; set; }

        public string error { get; set; }

        public Message() { }

        public Message(Type type)
        {
            this.type = type;
        }

        public virtual object ToJSON()
        {
            var json = new Dictionary<string, object>();

            json["type"] = type.ToString();
            json["error"] = error;

            return json;
        }

        public static void FromJSON(Message msg, object json)
        {
            msg.error = Util.JsonOptField(json as IDictionary<string, object>, "error") as string;
        }

        public override string ToString()
        {
            return Json.Serialize(ToJSON());
        }
    }
}
