using System;

namespace XSockets.Client40.Common.Interfaces
{
    public interface IXSocketJsonSerializer
    {
        //Serialization
        string SerializeToString<T>(T obj);
        string SerializeToString(object obj, Type type);

        //Deserialization
        dynamic DeserializeFromString(string json);
        T DeserializeFromString<T>(string json);
        object DeserializeFromString(string json, Type type);
    }
}
