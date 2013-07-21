using System;
using System.Collections.Generic;

using Sce.PlayStation.Core;
using Sce.PlayStation.Core.Environment;
using Sce.PlayStation.Core.Graphics;
using Sce.PlayStation.Core.Input;
using System.IO;
using Sce.PlayStation.Core.Imaging;


using C64Lib;

namespace C64Emu
{
	public class AppMain : EmulatorUI
	{
		AppMain() : base()
		{
		}
		
		protected override void drawOverlay()
		{
			drawText (statistics.FramesPerSecond + " fps", 10, 10, 0xffffffff);
			drawText ((int) EmulationSpeed + "%", ScreenWidth-10, 10, 0xffffffff, TextAlignment.RIGHT);
		}

        public static void Main(string[] args)
        {
            AppMain app = new AppMain();

            app.startup();
            app.Run();
            app.shutdown();


        }
    }
}
