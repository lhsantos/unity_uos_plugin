using MiniJSON;
using System.Collections.Generic;


namespace UOS
{
    public class Response : Message
    {
        public IDictionary<string, object> responseData { get; set; }
        public CallContext messageContext { get; set; }

        public Response()
        {
            type = Message.Type.SERVICE_CALL_RESPONSE;
        }

        public object getResponseData(string key)
        {
            object data;
            if ((responseData != null) && responseData.TryGetValue(key, out data))
                return data;
            else
                return null;
        }

        public string getResponseString(string key)
        {
            return (string)getResponseData(key);
        }

        public Response addParameter(string key, object value)
        {
            if (responseData == null)
                responseData = new Dictionary<string, object>();

            responseData[key] = value;

            return this;
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || (!(obj is Response)))
                return false;


            Response temp = (Response)obj;
            return
                (responseData == temp.responseData) ||
                ((responseData != null) && responseData.Equals(temp.responseData));
        }

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            if (responseData != null)
                hash ^= responseData.GetHashCode();

            return hash;
        }

        public override object ToJSON()
        {
            IDictionary<string, object> json = base.ToJSON() as IDictionary<string, object>;
            if (responseData != null)
                json["responseData"] = responseData;

            return json;
        }

        public static Response FromJSON(object json)
        {
            Response r = new Response();
            Message.FromJSON(r, json);

            r.responseData = Util.JsonOptField((IDictionary<string, object>)json, "responseData") as IDictionary<string, object>;

            return r;
        }

        public override string ToString()
        {
            return Json.Serialize(ToJSON());
        }
    }
}
