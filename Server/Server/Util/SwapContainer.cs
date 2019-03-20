using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Util
{
    class SwapContainer<T> where T : new()
    {
        public T In = new T();
        public T Out = new T();

        public readonly object Lock = new object();

        // thraed safe container
        public void Swap()
        {
            lock (Lock)
            {
                var temp = In;
                In = Out;
                Out = temp;
            }
        }
    }
}
