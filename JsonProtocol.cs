/// <summary>
/// A template for a really simple/stupid protocol.
/// The magic here is that clients connected to this protocol (TelNet, Arduino, Netduino or whatever) will be able
/// to communicate cross-protocol with client talking RFC6455 for example (or any other implemented protocol).
/// 
/// Override OnIncomingTextFrame and OnOutgoingTextFrame to implement custom parsing logic
/// </summary>
public class JsonProtocol : XSocketProtocol
{
    /// <summary>
    /// Extract the path (controller name) from the handshake
    /// </summary>
    public Regex GetPathRegex
    {
        get { return new Regex(@".+?(?= " + this.ProtocolPattern + ")", RegexOptions.None); }
    }

    /// <summary>
    /// A simple identifier fot the protocol since the ProtocolPattern might be complex or unfriendly to read.
    /// </summary>
    public override string ProtocolIdentifier
    {
        get { return "JsonProtocolIdentifier"; }
    }

    /// <summary>
    /// The string to identify the protocol in the handshake
    /// </summary>
    public override string ProtocolPattern
    {
        get { return "JsonProtocol"; }
    }

    /// <summary>
    /// The string to return after handshake
    /// </summary>
    public override string HostResponse
    {
        get { return "Welcome to JsonProtocol"; }
    }

    /// <summary>
    /// Perform any extra logic for handshake, build a hostresponse etc
    /// </summary>
    /// <returns></returns>
    public override bool DoHandshake()
    {
        Response = HostResponse;
        return true;
    }

    /// <summary>
    /// Set to true if your clients connected to this protocol will return pong on ping.
    /// </summary>
    /// <returns></returns>
    public override bool CanDoHeartbeat()
    {
        return false;
    }

    public override IXSocketProtocol NewInstance()
    {
        return new JsonProtocol();
    }

    public override IMessage OnIncomingFrame(List<byte> payload, MessageType messageType)
    {
        var data = Encoding.UTF8.GetString(payload.ToArray()).Replace("\r\n", string.Empty);
        if (string.IsNullOrEmpty(data)) return null;
        return base.OnIncomingFrame(payload, messageType);
    }
}