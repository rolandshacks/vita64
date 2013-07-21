using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace C64Lib
{
    public interface EmulatorIOAdapter
    {
        void onNewFrame(byte[] frameBuffer);
        int[] getKeyboardEvents();
    }
}
