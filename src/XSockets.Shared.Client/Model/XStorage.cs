using Newtonsoft.Json;

namespace XSockets.Model
{
    public class XStorage
    {
        /// <summary>
        /// The key value for the storage object
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "K")]
        public string Key { get; set; }

        /// <summary>
        /// The value of the storage object
        /// </summary>        
        [JsonProperty(PropertyName = "V")]
        public object Value { get; set; }

        public XStorage(){}
    }
}