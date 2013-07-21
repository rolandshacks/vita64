
using C64Lib.Memory.Base;

namespace C64Lib.Memory
{
    public class RamBytePointer
    {
        private int _address;
        private RamBase _memory;

        public RamBytePointer(RamBase memory, int address)
            : this(memory)
        {
            _address = address;
        }

        public RamBytePointer(RamBase memory)
        {
            _memory = memory;
        }

        public int Address
        {
            get { return _address; }
            set { _address = value; }
        }

        public byte this[int offset]
        {
            get { return _memory[_address + offset]; }
            set { _memory[_address + offset] = value; }
        }

        public byte Value
        {
            get { return _memory[_address]; }
            set { _memory[_address] = value; }
        }

        public byte[] GetValues(int length)
        {
            return _memory.Read(_address, length);
        }

        public void SetValues(byte[] values)
        {
            _memory.Write(_address, values);
        }


        public RamBytePointer NewPointerAtOffset(int offset)
        {
            return new RamBytePointer(_memory, _address + offset);
        }

    }
}
