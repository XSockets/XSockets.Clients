namespace XSockets.Model
{

    public enum QoS : byte
    {
        FireAndForget = 0x00,
        AtLeastOnce = 0x01,
        //ExactlyOnce = 0x02
    }
}
