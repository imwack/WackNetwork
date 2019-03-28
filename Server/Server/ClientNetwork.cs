using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Server.Util;

namespace Server
{
    class ConnectAsyncResult
    {
        public Exception Ex;
        public Connection Conn;
    }

    public class ClientNetwork:INetwork
    {

        public bool Connected { get { return connection != null && connection.Connected; } }

        public delegate void ClientNetworkConnectedHandler(ILocal local, Exception e);
        public delegate void ClientNetworkDisconnectedHandler();
        public delegate void ClientNetworkMessageReceivedHandler(Message msg);

        public ClientNetworkConnectedHandler OnConnected;
        public ClientNetworkDisconnectedHandler OnDisconnected;
        public ClientNetworkMessageReceivedHandler OnMessageReceived;


        private Connection connection;

        private ConnectAsyncResult defferedConnected = null;

        private readonly string hostIp;
        private readonly int hostPort;
        private readonly Socket Socket;

        public ClientNetwork(string ip, int port)
        {
            hostIp = ip;
            hostPort = port;

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                SendTimeout = 500,
                ReceiveTimeout = 500,
                NoDelay = true
            };
        }

        public void Connect()
        {
            if (Connected)
            {
                return;
            }

            Logger.Debug("ClientSocket.Connect({0}, {1});", hostIp, hostPort);
            try
            {
                Socket.Connect(hostIp, hostPort);
            }
            catch (Exception e)
            {
                Logger.Error("ClientNetwork BeginReceive throw exp:{0}", e);
                defferedConnected = new ConnectAsyncResult()
                {
                    Ex = e,
                };
                return;
            }

            defferedConnected = new ConnectAsyncResult()
            {
                Conn = new Connection(Socket, 0),
            };
        }

        public void SendData(byte[] buffer)
        {
            if (connection == null || !connection.Connected)
            {
                return;
            }

            connection.Push(buffer, buffer.Length);
        }

        public void SendDatav(params byte[][] buffers)
        {
            if (connection == null || !connection.Connected)
            {
                return;
            }
            connection.Pushv(buffers);
        }

        public void OneLoop()
        {
            try
            {
                if (defferedConnected != null)
                {
                    connection = defferedConnected.Conn;
                    connection.BeginReceive();

                    // notify
                    if (OnConnected != null)
                    {
                        OnConnected(defferedConnected.Conn, defferedConnected.Ex);
                    }
                    defferedConnected = null;
                }

                RefreshMessageQueue();
                RefreshClient();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public void Close()
        {
            if (!Connected)
            {
                return;
            }
            connection.Close();
            if (OnDisconnected != null)
            {
                OnDisconnected();
            }
        }

        //todo  RC4
        public void SetClientRc4Key(string key)
        {
            if (OnDisconnected != null)
            {
                //connector.RC4Key = key;
            }
        }

        public void Dispose()
        {
            Close();
        }

        //send data
        private void RefreshClient()
        {
            if (Connected)
            {
                if (!connection.DefferedClose)
                {
                    connection.Send();
                }

                if (connection.DefferedClose)
                {
                    Close();
                }
            }
        }

        //process data
        private void RefreshMessageQueue()
        {
            if (!Connected)
            {
                return;
            }

            connection.ProcessMessageQueue((c, msg) =>
            {
                if (OnMessageReceived != null)
                {
                    OnMessageReceived(msg);
                }
            });
        }
    }
}
