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
        private Socket Socket;
        private const int HeadLen = 4; //mark the buffer size
        public int connectionId;
        internal bool Connected { get; set; }

        public string RemoteIp
        {
            get
            {
                if (Socket != null)
                {
                    var ipEndPoint = Socket.RemoteEndPoint as IPEndPoint;
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
                if (Socket != null)
                {
                    var ipEndPoint = Socket.RemoteEndPoint as IPEndPoint;
                    if (ipEndPoint != null)
                    {
                        return ipEndPoint.Port;
                    }
                }
                return 0;
            }
        }

        public int Id { get; }

        public bool DefferedClose { get; set; } //close by deffer

        //Todo use RC4
        private bool useRC4 = false;
        private SKBuffer receiveBuffer = new SKBuffer();    //to receive data
        private SKBuffer sendBuffer = new SKBuffer();       //to send data

        private readonly SwapContainer<Queue<Message>> messageQueue = new SwapContainer<Queue<Message>>();
        public delegate void MessageHandler(IRemote remote, Message msg);

        //Construct
        public Connection(Socket socket, int id)
        {
            this.Socket = socket;
            this.connectionId = id;
            Connected = true;
        }

        public void ProcessMessageQueue(MessageHandler messageHandler)
        {
            if (!Connected)
            {
                return;
            }

            if (messageQueue.Out.Count == 0)
            {
                messageQueue.Swap();
            }

            while (messageQueue.Out.Count > 0)
            {
                var msg = messageQueue.Out.Dequeue();
                messageHandler(this, msg);
            }
        }

        public int Push(byte[] buffer, int len, int offset = 0)
        {
            if (!Connected)
            {
                return -1;
            }
            if (offset + len > buffer.Length)
            {
                return -2;
            }

            //todo encrypt
            var headData = new byte[HeadLen];
            Converter.Int32ToByteArray(len, headData, 0);
            sendBuffer.PushData(headData, headData.Length);
            sendBuffer.PushData(buffer, len, offset);
            return buffer.Length + HeadLen;
        }

        public int PushBegin(int len)
        {
            if (!Connected)
            {
                return -1;
            }

            var headData = new byte[HeadLen];
            Converter.Int32ToByteArray(len, headData, 0);
            sendBuffer.PushData(headData, headData.Length);
            return HeadLen;
        }

        public int PushMore(byte[] buffer, int len, int offset)
        {
            sendBuffer.PushData(buffer, len, offset);
            return len;
        }

        public int Pushv(params byte[][] buffers)
        {
            if (!Connected)
            {
                return -1;
            }

            int size = 0;
            foreach (var buffer in buffers)
            {
                size += buffer.Length;
            }

            //todo encrypt 
            var headData = new byte[HeadLen];
            Converter.Int32ToByteArray(size, headData, 0);
            sendBuffer.PushData(headData, headData.Length);
            foreach (var buffer in buffers)
            {
                sendBuffer.PushData(buffer, buffer.Length);
            }
            return size + HeadLen;
        }

        public void BeginReceive()
        {
            Receive();
        }

        public void Receive()
        {
            if (!Connected)
            {
                return;
            }
            try
            {
                if (receiveBuffer.EnsureFreeSpace(1))
                {
                    Socket.BeginReceive(receiveBuffer.Buffer, receiveBuffer.End, receiveBuffer.FreeSize, SocketFlags.None, OnReceivedCallback, this);
                }
                else
                {
                    DefferedClose = true;
                }
            }
            catch (SocketException e)
            {
                Logger.Error("Connection.Receive socket error={0}", e.Message);
            }
            catch (Exception e)
            {
                Logger.Error("Connection.Receive error={0}", e);
            }
        }

        public void Send()
        {
            if (sendBuffer.Length <= 0)
            {
                return;
            }

            try
            {
                int realSendLen = Socket.Send(sendBuffer.Buffer, sendBuffer.Start, sendBuffer.Length, SocketFlags.None);

                if (realSendLen == sendBuffer.Length)
                {
                    sendBuffer.Reset();
                    sendBuffer.TryShink();
                }
                else
                {
                    sendBuffer.Consume(realSendLen);
                }
            }
            catch (SocketException e)
            {
                Logger.Error("Connector.SendData error connectId={0} errorCode={1} msg={2}", Id, e.ErrorCode, e.Message);
                if (e.ErrorCode == 10054 || e.ErrorCode == 10053 || e.ErrorCode == 10058)
                {
                    // 10054 remote reset | 10053 Software caused connection abort |10058 socket shutdown.
                    sendBuffer.Reset();
                    DefferedClose = true;
                }
            }
            catch (Exception e)
            {
                sendBuffer.Reset();
                Logger.Error("Connector.SendData error={0}", e);
                DefferedClose = true;
            }
        }

        public void Close()
        {
            Connected = false;
            if (Socket != null)
            {
                Socket.Close();
                Socket = null;
            }

            receiveBuffer.Dispose();
            sendBuffer.Dispose();
            receiveBuffer = null;
            sendBuffer = null;
        }

        //process received data and add to message queue
        private void OnReceivedCallback(IAsyncResult ar)
        {
            int bytesRead = 0;
            try
            {
                if (Socket != null)
                {
                    bytesRead = Socket.EndReceive(ar);
                }
            }
            catch (ObjectDisposedException e)
            {
                Logger.Error("Connection.OnReceivedCallback objectDisposedException connectId={0}", Id);
                DefferedClose = true;
                return;
            }
            catch (SocketException e)
            {
                Logger.Error("Connection.OnReceivedCallback connectId={0} errorCode={1} errorMessage={2}", Id, e.ErrorCode, e.Message);
                if (e.ErrorCode == 10054 || e.ErrorCode == 10053 || e.ErrorCode == 10058)
                {
                    DefferedClose = true;
                }
                return;
            }
            catch (Exception ex)
            {
                // unknown errors
                Logger.Error("Connection.OnReceivedCallback exception connectId={0} ex={1}", Id, ex.ToString());
                DefferedClose = true;
                return;
            }

            if (bytesRead == 0)
            {
                Logger.Debug("Connection.OnReceivedCallback  read is 0 connectId={0}", Id);
                return;
            }

            receiveBuffer.Produce(bytesRead);
            while (receiveBuffer.Length >= HeadLen)
            {
                // todo a strange bug occurs here ever
                int size = BitConverter.ToInt32(receiveBuffer.Buffer, receiveBuffer.Start);
                if (size < 0)
                {
                    Logger.Warn("Connection.OnReceivedCallback size={0} id={1} buffer={2} bytesRead={3}", size, Id, receiveBuffer, bytesRead);
                    break;
                }

                if (receiveBuffer.Length >= size + HeadLen)
                {
                    byte[] destBuffer = null;
                    destBuffer = new byte[size];
                    Buffer.BlockCopy(receiveBuffer.Buffer, receiveBuffer.Start + HeadLen, destBuffer, 0, size);

                    // todo rc4
                    //if (useRC4)
                    //{
                    //    rc4Read.Encrypt(destBuffer, size);
                    //}

                    receiveBuffer.Reset();
                    try
                    {
                        MessageEnqueue(destBuffer);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }
                else
                {
                    // wait for remain data
                    break;
                }
            }
            Receive();
        }
        private void MessageEnqueue(byte[] buf)
        {
            var msg = new Message(buf);
            lock (messageQueue.Lock)
            {
                messageQueue.In.Enqueue(msg);
            }
        }
    }
}
