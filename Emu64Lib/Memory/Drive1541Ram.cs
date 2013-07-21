
using C64Lib.Memory.Base;

namespace C64Lib.Memory
{
    public class Drive1541Ram : RamBase
    {
        private const int _bamStartIndex = 0x700;

        public Drive1541Ram()
            : base(0x0800)
        
        { 
        }


        public int BamStartIndex
        {
            get { return _bamStartIndex; }
        }


    }
}
