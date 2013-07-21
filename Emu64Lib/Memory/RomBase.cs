
namespace C64Lib.Memory.Base
{
    public abstract class RomBase : MemoryBase
    {
        public RomBase(int size)
            : base(size)
        {  }

        public RomBase(byte[] data)
            : base(data.Length)
        {
            _bytes = data;
        }


        public byte this[int index]
        {
            get { return _bytes[index]; }
        }

    }
}
