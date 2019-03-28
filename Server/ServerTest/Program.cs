using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server;

namespace ServerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 3721;
            ServerNetwork server = new ServerNetwork(port);
            server.OnClientConnected = OnClientConnected;
            server.OnClientDisconnected = OnClientDisconnected;
            server.OnReceiveClientMessage = OnReceiveClientMessage;
            while (true)
            {
                server.OneLoop();
            }
        }

        private static void OnReceiveClientMessage(IRemote remote, Message msg)
        {
            Console.WriteLine($"[{DateTime.Now}][Debug]OnReceiveClientMessage {remote.RemoteIp} {remote.RemotePort} {msg}");
        }

        private static void OnClientDisconnected(IRemote remote)
        {
            Console.WriteLine($"[{DateTime.Now}][Debug]OnClientDisconnected {remote.RemoteIp} {remote.RemotePort}");
        }

        public static void OnClientConnected(IRemote remote)
        {
            Console.WriteLine($"[{DateTime.Now}][Debug]OnClientConnected {remote.RemoteIp} {remote.RemotePort}");
        }

    }
}
