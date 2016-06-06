using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XSockets.Common.Interfaces;

namespace XSockets.Helpers
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
    }
}
