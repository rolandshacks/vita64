using C64Lib.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace C64Lib
{
    public class EmulatorApplication
    {
        public struct VideoInfo
        {
            public int width;
            public int height;
            public int bitsPerPixel;
            public int size;
            public Color[] palette;
        }

        private C64 c64;

        public EmulatorApplication(EmulatorIOAdapter ioAdapter)
        {
            c64 = new C64(ioAdapter);

            _videoInfo = new VideoInfo();

            C64Display display = c64.Display;
            _videoInfo.width = display.getWidth();
            _videoInfo.height = display.getHeight();
            _videoInfo.bitsPerPixel = display.getBitsPerPixel();
            _videoInfo.size = _videoInfo.width * _videoInfo.height * _videoInfo.bitsPerPixel / 8;
            _videoInfo.palette = display.getPalette();
        }

        private VideoInfo _videoInfo;
        public VideoInfo Video
        {
            get { return _videoInfo; }
        }

        public void initialize()
        {
            c64.Run();
        }
		
		public float EmulationSpeed
		{
			get { return c64.EmulationSpeed; }
		}		

    }
}
