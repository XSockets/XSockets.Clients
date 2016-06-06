using System;

namespace XSockets.Common.Interfaces
{
    public interface IXSocketJsonSerializer
    {
        //Serialization
        string SerializeToString<T>(T obj);
        string SerializeToString(object obj, Type type);

        //Deserialization
        T DeserializeFromString<T>(string json);
        object DeserializeFromString(string json, Type type);
    }
}
