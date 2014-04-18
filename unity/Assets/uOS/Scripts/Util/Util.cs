using System;
using System.Collections;
using System.Collections.Generic;


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

        public static object JsonOptField(IDictionary<string, object> json, string field)
        {
            object obj = null;
            if (json.TryGetValue(field, out obj))
                return obj;

            return null;
        }
    }
}
