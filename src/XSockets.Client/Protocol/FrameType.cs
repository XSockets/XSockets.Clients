﻿namespace XSockets.Protocol
{
    public enum FrameType
    {
        Continuation = 0,
        Text = 1,
        Binary = 2,
        Close = 8,
        Ping = 9,
        Pong = 10,
    }
}