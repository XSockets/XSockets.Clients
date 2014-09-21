using System;
using System.Collections;
using System.Reflection;
using System.Text;
using XSockets.ClientMF43.Event.Arguments.Interfaces;
using XSockets.ClientMF43.Interfaces;
using XSockets.ClientMF43.Model;

namespace XSockets.ClientMF43.Helpers
{
    public static class Helpers
    {
        public static bool StartsWith(this string s, string value)
        {
            return s.IndexOf(value) == 0;
        }

        public static bool Contains(this string s, string value)
        {
            return s.IndexOf(value) > 0;
        }

        public static string GetString(this Encoding encoding, byte[] data) 
        {
            var sb = new StringBuilder();
            foreach (var c in encoding.GetChars(data)) 
            {
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static byte[] Take(this byte[] buffer, int length)
        {
            var b = new byte[length];
            for (var i = 0; i < length; i++)
                b[i] = buffer[i];
            return b;
        }

        public static IMessage ToMessage(this IXSocketClient c, byte[] b)
        {
            try
            {
                var json = UTF8Encoding.UTF8.GetString(b);
                var t = c.Serializer.Deserialize(json) as Hashtable;
                return new Message(t["D"] as string, t["T"] as string, t["C"] as string);
            }
            catch
            {
                return null;
            }           
        }

        public static object Parse(this IXSocketClient c, string json, Type t)
        {
            return (c.Serializer.Deserialize(json) as Hashtable).Parse(t);
        }

        public static object Parse(this Hashtable h, Type t)
        {
            var types = new Type[0];
            ConstructorInfo constructor = t.GetConstructor(types);
            var o = constructor.Invoke(null);

            foreach (var k in h.Keys)
            {
                t.GetMethod("set_" + k).Invoke(o, new object[] { h[k] });
            }

            return o;
        }
    }
}
