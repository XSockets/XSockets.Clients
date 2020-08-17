
namespace XSockets.Common.Interfaces
{
    using System;

    public interface IXSocketJsonSerializer
    {
        string SerializeToString<T>(T obj);
        string SerializeToString(object obj, Type type);
        bool IsValidJson(string strInput);
        dynamic DeserializeFromString(string json);
        T DeserializeFromString<T>(string json);
        object DeserializeFromString(string json, Type type);
    }
}
