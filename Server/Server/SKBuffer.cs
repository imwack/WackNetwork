using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Util;

namespace Server
{
    //Socket buffer
    //not a real skbuff structure it's complex, just like STL vector buffer with auto resize
    public class SKBuffer
    {
        private byte[] _buffer = new byte[1024];
        private int _start;
        private int _end;

        public byte[] Buffer
        {
            get { return _buffer; }
        }
        public int Length
        {
            get { return _end - _start; }
        }

        public int Start
        {
            get { return _start; }
        }

        public int End
        {
            get { return _end; }
        }
        public int FreeSize
        {
            get { return _buffer.Length - _end; }
        }

        public void PushData(byte[] data, int size, int offset = 0)
        {
            Resize(size);
            System.Buffer.BlockCopy(data, offset, _buffer, _end, size);
            _end += size;
        }

        public void Consume(int size)
        {
            _start += size;
        }

        public void Produce(int size)
        {
            _end += size;
        }

        private void Resize(int size)
        {
            int newSize = _buffer.Length;
            while (newSize - _end < size)
            {
                newSize *= 2;
            }
            if (newSize > _buffer.Length) //need resize
            {
                byte[] newBuffer = new byte[newSize];
                if (_end > 0)
                {
                    if (_end <= _buffer.Length)
                    {
                        var buffLen = Length;
                        System.Buffer.BlockCopy(_buffer, _start, newBuffer, 0, buffLen); //copy old buffer to new buffer front
                        _start = 0;
                        _end = buffLen;
                    }
                    else
                    {
                        Logger.Error("Resize fail _end={0} Length={1}", _end, _buffer.Length);
                    }
                }
                _buffer = newBuffer;
            }
        }
        public bool EnsureFreeSpace(int free)
        {
            Resize(free);
            return true;
        }
        public void Dispose()
        {
            Reset();
            _buffer = null;
        }
        public void Reset()
        {
            _end = 0;
            _start = 0;
        }

        public void TryShink()
        {
            if (Start != End || Buffer.Length < 32768)
            {
                return;
            }
            byte[] newBuffer = new byte[Buffer.Length / 2];
            _buffer = newBuffer;
        }

        public override string ToString()
        {
            return $"{{buffer.Len:{_buffer.Length} position:{_end} start:{_start}}}";
        }
    }
}
