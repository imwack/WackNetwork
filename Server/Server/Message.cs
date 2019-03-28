using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Message
    {
        public byte[] Buffer = null;
        public Message(byte[] buf)
        {
            Buffer = buf;
        }

        public override string ToString()
        {
            if (Buffer == null)
            {
                return "";
            }
            string s = "";
            for (int i = 0; i < Buffer.Length; i++)
            {
                s += Buffer[i];
                if (i != Buffer.Length - 1)
                {
                    s += ",";
                }
            }
            return s;
        }
    }
}
