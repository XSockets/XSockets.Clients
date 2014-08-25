namespace XSockets.ClientAndroid.Globals
{
    public static class Constants
    {
        public static class Connection
        {
            public static class Parameters
            {
                public const string PersistentId = "PersistentId";                
            }            
        }

        public static class Events
        {
            public const string Error = "0x1f4";

            public static class PubSub
            {
                public const string Subscribe = "0x12c";
                public const string Unsubscribe = "0x12d";
            }

            public static class Controller
            {
                public const string Opened = "0x14";
                public const string Closed = "0x15";
                public const string Init = "0xcc";
            }

            public static class Storage
            {
                public const string Set = "0x190";
                public const string Get = "0x191";
                public const string Clear = "0x192";
                public const string Remove = "0x193";
            }
        }

        public static class WebSocketFields
        {
            public const string SecWebsocketKey = "sec-websocket-key";
            public const string SecWebsocketKey1 = "sec-websocket-key1";
            public const string SecWebsocketKey2 = "sec-websocket-key2"; 
            public const string SecWebsocketProtocol = "sec-websocket-protocol";
            public const string Path = "path";
            public const string Origin = "origin";
            public const string Host = "host";
        }
    }
}