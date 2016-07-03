using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject
{
    class Program
    {
        static void Main(string[] args)
        {
            // 
            Task.Factory.StartNew(() =>
            {
                doStuff();
            });

            Console.ReadLine();
        }
        private static async Task doStuff()
        {
            Console.WriteLine("...");
            var c = new XSockets.XSocketClient("ws://localhost:4502", "http://localhost", "generic");
            c.OnConnectAttempt += (s, e) => { Console.WriteLine("ConnectAttempt"); };
            c.OnAutoReconnectFailed += (s, e) => { Console.WriteLine("ReconnectFailed"); };
            c.OnConnected += (s, e) => { Console.WriteLine("Connected"); };
            c.OnDisconnected += (s, e) =>
            {
                Console.WriteLine("Disconnect");
            };
            c.SetAutoReconnect();
            Console.WriteLine("...");
            var r = await c.Open();
            Console.WriteLine("..." + r);
        }
    }
}
