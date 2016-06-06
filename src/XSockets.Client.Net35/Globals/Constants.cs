namespace XSockets.Globals
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
            public const string Error = "4";
            public const string AuthenticationFailed = "0";
            public const string Ping = "7";
            public const string Pong = "8";
            public static class PubSub
            {
                public const string Subscribe = "5";
                public const string Unsubscribe = "6";
            }

            public static class QoS
            {
                public const string MsgAck = "9";
                public const string MsgRel = "10";

                public const string MsgRec = "11";
                public const string MsgComp = "12";
                //PubSub move to pub/sub?
                //public const string SubAck = "13";
                //public const string UnsubAck = "14";

                public static int RetryInterval = 1000;
            }

            public static class Controller
            {
                public const string Opened = "2";
                public const string Closed = "3";
                public const string Init = "1";
            }

            public static class Storage
            {
                public const string Set = "s1";
                public const string Get = "s2";
                public const string Clear = "s4";
                public const string Remove = "s3";
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