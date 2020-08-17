
namespace XSockets.Helpers
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Common.Interfaces;

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
            if (typeof(T) == typeof(string) && !this.IsValidJson(json))
                json = this.SerializeToString(json);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public object DeserializeFromString(string json, Type type)
        {
            if (type == typeof(string) && !this.IsValidJson(json))
                json = this.SerializeToString(json);
            return JsonConvert.DeserializeObject(json, type, new JsonSerializerSettings());
        }

        public bool IsValidJson(string strInput)
        {
            //if (json.StartsWith("\"") && json.EndsWith("\""))
            //{
            //    var o = ser.DeserializeFromString<string>(json);
            //    var obj = JToken.Parse(json);
            //    Console.WriteLine(json);
            //    Console.WriteLine(obj);
            //}

            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]")) || //For array
                (strInput.StartsWith("\"") && strInput.EndsWith("\""))) //For JSON string value
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch
                {
                    //Exception in parsing json                    
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public dynamic DeserializeFromString(string json)
        {
            return JObject.Parse(json);            
        }
    }
}
