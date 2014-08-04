using System;
using System.Runtime.Serialization;

namespace XSockets.ClientAndroid.Model
{
    [Serializable]
    [DataContract]
    public class XStorage
    {
        /// <summary>
        /// The key value for the storage object
        /// </summary>
        [DataMember(IsRequired = true, Name = "K")]
        public string Key { get; set; }

        /// <summary>
        /// The value of the storage object
        /// </summary>
        [DataMember(IsRequired = false, Name = "V")]
        public object Value { get; set; }

        public XStorage(){}
    }
}