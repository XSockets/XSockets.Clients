using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using XSockets.ClientIOS.Common.Interfaces;
using XSockets.ClientIOS.Model;

namespace XSockets.ClientIOS.Helpers
{    
    public static partial class XSocketHelper
    {
        #region "Transformation Methods - XSocketEvents & JSON"

        /// <summary>
        /// If possible use the extension-method ToTextArgs for the controller instead
        /// </summary>
        /// <param name="client"></param>
        /// <param name="obj"></param>
        /// <param name="eventname"></param>
        /// <returns></returns>
        public static IMessage AsMessage(this IXSocketClient client, object obj, string eventname)
        {
            return new Message(obj,eventname);
        }

        /// <summary>
        /// Builds a ITextArgs object from a JSON string and a event name
        /// </summary>
        /// <param name="json"></param>
        /// <param name="eventname"></param>
        /// <returns></returns>
        public static IMessage AsTextArgsForJson(this string json, string eventname)
        {
            return new Message(json,eventname);
        }

        /// <summary>
        /// Use when sending binary data
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="topic"></param>
        /// <param name="controller"></param>
        /// <returns></returns>
        public static IMessage AsMessage(this byte[] obj, string topic, string controller ="")
        {            
            return new Message(obj, topic, controller);
        }

        /// <summary>
        /// Deserialize JSON to a strongly typed object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T Deserialize<T>(this IXSocketClient client, string json)
        {
            return client.Serializer.DeserializeFromString<T>(json);
        }

        /// <summary>
        /// Deserialize JSON to a strongly typed object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T Deserialize<T>(this string json)
        {
            var serializer = new XSocketJsonSerializer();
            return serializer.DeserializeFromString<T>(json);
        }

        /// <summary>
        /// If possible use the extension-method ToTextArgs for the controller instead
        /// </summary>
        /// <param name="client"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string Serialize(this IXSocketClient client, object obj)
        {
            return client.Serializer.SerializeToString(obj);
        }

        /// <summary>
        /// Deserialize JSON to a strongly typed object.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="targetType"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        internal static object GetObject(this IXSocketClient client, Type targetType, string json)
        {
            return client.Serializer.DeserializeFromString(json, targetType);
        }
        #endregion

        public static String ConstructQueryString(this NameValueCollection parameters)
        {
            if (parameters.Count == 0) return string.Empty;

            List<string> items = new List<string>();

            foreach (string name in parameters)
                items.Add(String.Concat(name, "=", parameters[name].UrlEncode()));

            return "?"+String.Join("&", items.ToArray());
        }

        public static string UrlEncode(this string s)
        {
            return Uri.EscapeDataString(s).Replace("%20", "+");
        }

        private static readonly ConcurrentDictionary<string, bool> TypeDictionary = new ConcurrentDictionary<string, bool>(); 

        internal static bool IsBuiltIn(this Type type)
        {
            var typename = type.FullName;
            if (TypeDictionary.ContainsKey(typename)) return TypeDictionary[typename];

            if (type.IsGenericType) return false;

            if (type.Namespace != null && (type.Namespace.StartsWith("System") && (type.Module.ScopeName == "CommonLanguageRuntimeLibrary" || type.Module.ScopeName == "mscorlib.dll")))
            {
                TypeDictionary.TryAdd(typename, true);
                return true;
            }
            TypeDictionary.TryAdd(typename, false);
            return false;
        }
    }
}