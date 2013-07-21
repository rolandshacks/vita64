
using C64Lib.Memory.Base;
using System;

namespace C64Lib.Memory
{
    public class ColorRam : RamBase
    {
        public ColorRam()
            : base(0x0400)
        { }

        public void InitializeWithRandomValues()
        {
            Random rand = new Random();

            for (int i = 0; i < 1024; i++)
                _bytes[i] = (byte)(rand.Next() & 0x0f);
        }




    }
}
