using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Connection
    {
        private Socket clientSocket;
        private int connectionId;

        public Connection(Socket clientSocket, int id)
        {
            this.clientSocket = clientSocket;
            this.connectionId = id;
        }
    }
}
