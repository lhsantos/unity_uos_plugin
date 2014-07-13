using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace UOS
{
    public static class Util
    {
        public static bool Compare(object a, object b)
        {
            if (a == b)
                return true;

            if ((a != null) && (b != null) && (a is ICollection) && (b is ICollection))
            {
                // This comparison is O(N^2) because Unity only allows a subset of .NET
                ICollection ca = (ICollection)a;
                ICollection cb = (ICollection)b;

                if (ca.Count != cb.Count)
                    return false;

                foreach (var ea in ca)
                {
                    bool found = false;
                    foreach (var eb in cb)
                    {
                        if (Compare(ea, eb))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        return false;
                }

                return true;
            }

            return (a != null && a.Equals(b));
        }

        public static void JsonPut(IDictionary<string, object> json, string key, object val)
        {
            if (val == null)
                json.Remove(key);
            else
                json[key] = val;
        }

        public static object JsonOptField(IDictionary<string, object> json, string field)
        {
            object obj = null;
            if (json.TryGetValue(field, out obj))
                return obj;

            return null;
        }

        public static string JsonOptString(IDictionary<string, object> json, string field, string defaultValue = null)
        {
            try
            {
                return JsonOptField(json, field) as string;
            }
            catch
            {
                return defaultValue;
            }
        }

        public static int JsonOptInt(IDictionary<string, object> json, string field, int defaultValue)
        {
            try
            {
                object obj = null;
                if (json.TryGetValue(field, out obj))
                {
                    if (obj is Int64)
                        return (int)(Int64)obj;
                    else if (obj is Int32)
                        return (int)(Int32)obj;
                    else if (obj is string)
                        return int.Parse(obj as string);
                }
            }
            catch { }

            return defaultValue;
        }

        public static T JsonOptEnum<T>(IDictionary<string, object> json, string field, T defaultValue) where T : struct, IConvertible
        {
            Type type = typeof(T);

            if (!type.IsEnum)
                throw new InvalidOperationException("Enum type expected!");

            try
            {
                object obj = null;
                if (json.TryGetValue(field, out obj))
                    return (T)Enum.Parse(type, obj as string, true);
            }
            catch { }

            return defaultValue;
        }

        public static string JsonStructure(object json, string ident = "")
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(ident);
            if (json is IDictionary<string, object>)
            {
                builder.Append("{\n");
                IDictionary<string, object> dic = json as IDictionary<string, object>;
                foreach (var pair in dic)
                {
                    builder.Append(ident);
                    builder.Append("\t");
                    builder.Append(pair.Key);
                    builder.Append(":\n");
                    builder.Append(JsonStructure(pair.Value, ident + "\t\t"));
                    builder.Append("\n");
                }
                builder.Append(ident);
                builder.Append("}");
            }
            else if (json is List<object>)
            {
                builder.Append("[\n");
                List<object> list = json as List<object>;
                for (int i = 0; i < list.Count; ++i)
                {
                    builder.Append(JsonStructure(list[i], ident + "  "));
                    if (i < list.Count - 1)
                        builder.Append(",");
                    builder.Append("\n");
                }
                builder.Append(ident);
                builder.Append("]");
            }
            else
                builder.Append(json.ToString());

            return builder.ToString();
        }


        public static string GetHost(string networkDeviceName)
        {
            return networkDeviceName.Split(':')[0];
        }

        public static string GetPort(string networkDeviceName)
        {
            return networkDeviceName.Split(':')[1];
        }

        public static T ConvertOrParse<T>(object v)
        {
            if (v is T)
                return (T)v;
            else
            {
                Type t = typeof(T);
                var method = t.GetMethod("Parse", new Type[] { typeof(string) });
                if ((method != null) && method.IsPublic && method.IsStatic)
                {
                    try { return (T)method.Invoke(null, new object[] { v.ToString() }); }
                    catch (System.Reflection.TargetInvocationException e) { throw e.InnerException; }
                }
                else
                    throw new System.ArgumentException("Cannot convert or parse this value.");
            }
        }

        public static System.Type GetType(string typeName)
        {
            var type = System.Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        public static IPAddress[] GetLocalIPs()
        {
            return Array.FindAll<IPAddress>(
                    Dns.GetHostEntry(Dns.GetHostName()).AddressList,
                    a => a.AddressFamily == AddressFamily.InterNetwork
                );
        }
    }
}
