using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Server;

namespace ClientTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string ip = "127.0.0.1";
            int port = 3721;

            byte[] data = BitConverter.GetBytes(123);     // 将 int 转换成字节数组

            ClientNetwork client = new ClientNetwork(ip, port);
            client.OnConnected = OnConnected;
            client.OnDisconnected = OnDisconnected;
            client.OnMessageReceived = OnMessageReceived;
            client.Connect();
            while (true)
            {
                client.OneLoop();
                client.SendData(data);
                Thread.Sleep(100);
            }
        }

        private static void OnMessageReceived(Message msg)
        {
            Console.WriteLine($"[{DateTime.Now}][Debug]OnMessageReceived {msg.ToString()} ");
        }

        private static void OnDisconnected()
        {
            Console.WriteLine($"[{DateTime.Now}][Debug]OnDisconnected ");
        }

        private static void OnConnected(ILocal local, Exception e)
        {
            Console.WriteLine($"[{DateTime.Now}][Debug]OnConnected {local.RemoteIp} {local.RemotePort}");
        }
    }
}
