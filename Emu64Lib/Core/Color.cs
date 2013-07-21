using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C64Lib.Core
{
    public class Color
    {
        public byte R;
        public byte G;
        public byte B;

        public Color(int r, int g, int b)
        {
            this.R = (byte) r;
            this.G = (byte)g;
            this.B = (byte)b;
        }

        public static Color FromArgb(int a, int r, int g, int b)
        {
            return new Color(r, g, b);
        }

        public static Color FromRgb(int r, int g, int b)
        {
            return new Color(r, g, b);
        }

        public int ToBrg32()
        {
            int brg = (R << 16);
            brg |= (G << 8);
            brg |= (B << 0);

            return brg;
        }
    }
}
