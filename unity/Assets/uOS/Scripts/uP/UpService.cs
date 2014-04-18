using MiniJSON;
using System.Collections.Generic;


namespace UOS
{
    public class UpService
    {
        public enum ParameterType
        {
            MANDATORY,
            OPTIONAL
        }

        public string name { get; set; }
        public IDictionary<string, ParameterType> parameters { get; set; }


        public UpService() { }

        public UpService(string name)
        {
            this.name = name;
        }

        public UpService AddParameter(string paramName, ParameterType paramType)
        {
            if (parameters == null)
                parameters = new Dictionary<string, ParameterType>();

            parameters[paramName] = paramType;

            return this;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is UpService))
                return false;

            UpService d = (UpService)obj;

            return name.Equals(d.name);
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

            json["name"] = name;
            AddParameters(json, "parameters");

            return json;
        }

        private void AddParameters(IDictionary<string, object> json, string propName)
        {
            IDictionary<string, object> j_params = new Dictionary<string, object>();
            json[propName] = j_params;

            if (parameters != null)
            {
                foreach (var p in parameters)
                    j_params[p.Key] = p.Value.ToString();
            }
        }

        public static UpService FromJSON(object obj)
        {
            IDictionary<string, object> json = obj as IDictionary<string, object>;
            UpService s = new UpService();

            s.name = json["name"] as string;

            IDictionary<string, object> p_map = Util.JsonOptField(json, "parameters") as IDictionary<string, object>;
            if (p_map != null)
            {
                foreach (var p in p_map)
                {
                    ParameterType parameterType = (ParameterType)System.Enum.Parse(typeof(ParameterType), p.Value as string, true);
                    s.AddParameter(p.Key, parameterType);
                }
            }

            return s;
        }
    }
}
