namespace XSockets.ClientMF42.Event.Arguments.Interfaces
{
    public interface IMessage
    {        
        string D { get; }
        MessageType MessageType { get; }
        string C { get; set; }
        string T { get; set; }     
        string ToString();
        byte[] ToBytes();
    }
}