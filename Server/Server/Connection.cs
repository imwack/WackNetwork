using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Server.Util;

namespace Server
{
    public interface IRemote
    {
        string RemoteIp { get; }
        int RemotePort { get; }
        int Id { get; }

        int Push(byte[] buffer, int len, int offset);
        int PushBegin(int len);
        int PushMore(byte[] buffer, int len, int offset);
    }

    public interface ILocal
    {
        string RemoteIp { get; }
        int RemotePort { get; }
    }

    internal class Connection : IRemote, ILocal
    {
        private Socket clientSocket;
        public int connectionId;


        internal bool Connected { get; set; }

        public string RemoteIp
        {
            get
            {
                if (clientSocket != null)
                {
                    var ipEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
                    if (ipEndPoint != null)
                    {
                        return ipEndPoint.Address.ToString();
                    }
                }
                return "";
            }
        }

        public int RemotePort
        {
            get
            {
                if (clientSocket != null)
                {
                    var ipEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
                    if (ipEndPoint != null)
                    {
                        return ipEndPoint.Port;
                    }
                }
                return 0;
            }
        }

        public bool DefferedClose { get; set; }

        private ConnectionBuffer receiveBuffer = new ConnectionBuffer();
        private ConnectionBuffer sendBuffer = new ConnectionBuffer();

        private readonly SwapContainer<Queue<Message>> messageQueue = new SwapContainer<Queue<Message>>();


        public Connection(Socket clientSocket, int id)
        {
            this.clientSocket = clientSocket;
            this.connectionId = id;
            Connected = true;
        }

        public void ProcessMessageQueue(Action<IRemote , Message> action)
        {
            throw new NotImplementedException();
        }

        public int Push(byte[] buffer, int len, int offset)
        {
            throw new NotImplementedException();
        }

        public int PushBegin(int len)
        {
            throw new NotImplementedException();
        }

        public int PushMore(byte[] buffer, int len, int offset)
        {
            throw new NotImplementedException();
        }

        public void BeginReceive()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Send()
        {
            throw new NotImplementedException();
        }
    }
}
