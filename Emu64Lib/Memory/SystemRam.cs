
using C64Lib.Memory.Base;

namespace C64Lib.Memory
{
    public class SystemRam : RamBase
    {
        public SystemRam()
            : base(0x10000)
        {
        }

        public void InitializeWithPowerUpPattern()
        {
            int p = 0;

            for (int i = 0; i < 512; i++)
            {
                for (int j = 0; j < 64; j++)
                    _bytes[p++] = 0;

                for (int k = 0; k < 64; k++)
                    _bytes[p++] = 0xff;
            }
        }


    }
}
