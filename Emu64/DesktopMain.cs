#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace C64Emu
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public class AppMain : EmulatorUI
    {
        AppMain() : base()
        {
        }

        protected override void drawOverlay()
        {
            drawText(statistics.FramesPerSecond + " fps", 10, 10, 0xffffffff);
            drawText((int)EmulationSpeed + "%", ScreenWidth - 10, 10, 0xffffffff, TextAlignment.RIGHT);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppMain app = new AppMain();

            app.startup();
            app.Run();
            app.shutdown();

            /*
            using (var game = new EmulatorUI())
            {
                game.Run();
            }
            */
        }
    }
#endif
}
