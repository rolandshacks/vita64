
using System;
namespace C64Lib.Memory.Base
{
    public abstract class RamBase : MemoryBase
    {
        public RamBase(int size)
            : base(size)
        { }

        public void MemCopy(int destinationAddress, int sourceAddress, int count)
        {
            System.Diagnostics.Debug.Assert(count > 0, String.Format("Bad count {0} passed in to MemCopy", count));
            System.Diagnostics.Debug.Assert(destinationAddress + count <= _bytes.GetUpperBound(0), String.Format("Bad count {0} passed in to MemCopy. Would overrun memory.", count));

            while (count-- > 0)
            {
                _bytes[destinationAddress++] = _bytes[sourceAddress++];
            }
        }

        public void MemSet(int destinationAddress, byte value, int count)
        {
            System.Diagnostics.Debug.Assert(count > 0, String.Format("Bad count {0} passed in to MemSet", count));
            System.Diagnostics.Debug.Assert(destinationAddress + count <= _bytes.GetUpperBound(0), String.Format("Bad count {0} passed in to MemSet. Would overrun memory.", count));

            while (count-- > 0)
            {
                _bytes[destinationAddress++] = value;
            }
        }

        public void Clear(int address, int length)
        {
            System.Diagnostics.Debug.Assert(address >= 0, String.Format("Bad index {0} passed in to Clear", address));
            System.Diagnostics.Debug.Assert(length <= _bytes.Length, String.Format("Bad length {0} passed in to Clear", length));

            Array.Clear(_bytes, address, length);
        }

        public void Clear()
        {
            Array.Clear(_bytes, 0, _bytes.Length);
        }


        public byte this[int address]
        {
            get { return _bytes[address]; }
            set { _bytes[address] = value; }
        }

        public byte this[RamBytePointer pointer]
        {
            get { return _bytes[pointer.Address]; }
            set { _bytes[pointer.Address] = value; }
        }


        public void Write(int address, byte[] bytes)
        {
            bytes.CopyTo(_bytes, address);
        }


        public RamBytePointer NewBytePointer(int address)
        {
            return new RamBytePointer(this, address);
        }


    }
}
