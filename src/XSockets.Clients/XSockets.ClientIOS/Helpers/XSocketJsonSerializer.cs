using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XSockets.ClientIOS.Common.Interfaces;

namespace XSockets.ClientIOS.Helpers
{
    public class XSocketJsonSerializer : IXSocketJsonSerializer
    {
        public XSocketJsonSerializer()
        {
            
        }
        public string SerializeToString<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public string SerializeToString(object obj, Type type)
        {
            return JsonConvert.SerializeObject(obj, type, Formatting.None, new JsonSerializerSettings());
        }

        public T DeserializeFromString<T>(string json)
        {           
            return JsonConvert.DeserializeObject<T>(json);
        }

        public object DeserializeFromString(string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type, new JsonSerializerSettings());
        }

        public dynamic DeserializeFromString(string json)
        {
            return JObject.Parse(json);            
        }
    }
}
