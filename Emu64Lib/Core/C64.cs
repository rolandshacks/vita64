
using C64Lib.Memory;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace C64Lib.Core
{
    public class C64
    {

        #region Private Fields

        BasicRom _basicRom;
        KernalRom _kernalRom;
        CharacterRom _characterRom;

        SystemRam _ram;
        ColorRam _colorRam;

        //byte[] _Basic, _Kernal,
        //      _Char, _Color;		// C64

        Drive1541Ram _ram1541;
        Drive1541Rom _rom1541;
        //byte[] _RAM1541;
        //byte[] _ROM1541;	// 1541

        C64Display _TheDisplay;

        MOS6510 _TheCPU;			// C64
        MOS6569 _TheVIC;
        MOS6581 _TheSID;
        MOS6526_1 _TheCIA1;
        MOS6526_2 _TheCIA2;
        IEC _TheIEC;
        REU _TheREU;
        MOS6502_1541 _TheCPU1541;	// 1541
        Job1541 _TheJob1541;

        UInt32 _CycleCounter;

        bool thread_running;	    // Emulation thread is running
        bool quit_thyself;		    // Emulation thread shall quit
        bool have_a_break;		    // Emulation thread shall pause

        int joy_minx, joy_maxx,
            joy_miny, joy_maxy;     // For dynamic joystick calibration
        byte joykey;			    // Joystick keyboard emulation mask value

        byte orig_kernal_1d84,	    // Original contents of kernal locations $1d84 and $1d85
              orig_kernal_1d85;	    // (for undoing the Fast Reset patch)

        int skipped_frames;			// number of skipped frames
        int timer_every;			// frequency of timer in frames
        int frame;					// current frame number
        byte joy_state;			    // Current state of joystick
        bool state_change;
		float emulationSpeed;

        HiResTimer frameTimer = HiResTimer.StartNew();

        #endregion

        #region Constants

        const UInt32 FRAME_INTERVAL = (1000000 / MOS6569.SCREEN_FREQ);	// in microseconds
        const UInt32 JOYSTICK_SENSITIVITY = 40;			// % of live range
        const UInt32 JOYSTICK_MIN = 0x0000;			// min value of range
        const UInt32 JOYSTICK_MAX = 0xffff;			// max value of range
        const UInt32 JOYSTICK_RANGE = (JOYSTICK_MAX - JOYSTICK_MIN);

        #endregion

        public C64Display Display
        {
            get { return _TheDisplay; }
        }

        #region Public methods

        public C64(EmulatorIOAdapter ioAdapter)
        {
            int i, j;
			
			emulationSpeed = 0.0f;

            // The thread is not yet running
            thread_running = false;
            quit_thyself = false;
            have_a_break = false;

            // Open display
            TheDisplay = new C64Display(this, ioAdapter);

            // Allocate RAM/ROM memory
            //RAM = new byte[0x10000];

            _ram = new SystemRam();
            _colorRam = new ColorRam();

            _basicRom = new BasicRom();
            _kernalRom = new KernalRom();
            _characterRom = new CharacterRom();

            //Basic = new byte[0x2000];
            //Kernal = new byte[0x2000];
            //Char = new byte[0x1000];
            //Color = new byte[0x0400];

            _ram1541 = new Drive1541Ram();
            _rom1541 = new Drive1541Rom();

            //RAM1541 = new byte[0x0800];
            //ROM1541 = new byte[0x4000];

            // Create the chips
            TheCPU = new MOS6510(this, RAM, Basic, Kernal, Char, Color);

            TheJob1541 = new Job1541(_ram1541);
            TheCPU1541 = new MOS6502_1541(this, TheJob1541, TheDisplay, _ram1541, _rom1541);

            TheVIC = TheCPU.TheVIC = new MOS6569(this, TheDisplay, TheCPU, RAM, Char, Color);
            TheSID = TheCPU.TheSID = new MOS6581(this);
            TheCIA1 = TheCPU.TheCIA1 = new MOS6526_1(TheCPU, TheVIC);
            TheCIA2 = TheCPU.TheCIA2 = TheCPU1541.TheCIA2 = new MOS6526_2(TheCPU, TheVIC, TheCPU1541);
            TheIEC = TheCPU.TheIEC = new IEC(TheDisplay);
            TheREU = TheCPU.TheREU = new REU(TheCPU);

            _ram.InitializeWithPowerUpPattern();
            _colorRam.InitializeWithRandomValues();

            TheDisplay.Initialize();

#if false
            //unsafe
            //{
            //    fixed (byte* pRAM = RAM, pColor = Color)
            //    {
            //        byte* p = pRAM;
            //        // Initialize RAM with powerup pattern
            //        for (i = 0; i < 512; i++)
            //        {
            //            for (j = 0; j < 64; j++)
            //                *p++ = 0;
            //            for (j = 0; j < 64; j++)
            //                *p++ = 0xff;
            //        }

            //        Random rand = new Random();
            //        p = pColor;
            //        // Initialize color RAM with random values
            //        for (i = 0; i < 1024; i++)
            //            *p++ = (byte)(rand.Next() & 0x0f);
            //    }
            //}
#endif

            // Open joystick drivers if required
            open_close_joysticks(false, false, GlobalPrefs.ThePrefs.Joystick1On, GlobalPrefs.ThePrefs.Joystick2On);
            joykey = 0xff;

            CycleCounter = 0;

            // No need to check for state change.
            state_change = false;

            // TODO
            frameTimer.Reset();
            frameTimer.Start();

        }

        internal void Initialize()
        {

        }

        public void Run()
        {
            TheCPU.Reset();
            TheSID.Reset();
            TheCIA1.Reset();
            TheCIA2.Reset();
            TheCPU1541.Reset();

            // Patch kernal IEC routines
            orig_kernal_1d84 = Kernal[0x1d84];
            orig_kernal_1d85 = Kernal[0x1d85];
            patch_kernel(GlobalPrefs.ThePrefs.FastReset, GlobalPrefs.ThePrefs.Emul1541Proc);

            // TODO
            //Events.Quit += new QuitEventHandler(Events_Quit);

            // Start the machine main loop
            //MainLoop();


            Thread t = new Thread(new ThreadStart(MainLoop));
            t.Start();
            
        }

        // TODO
        //void Events_Quit(object sender, QuitEventArgs e)
        //{
        //    Debug.WriteLine("Quit requested");

        //    Quit();
        //    Events.QuitApplication();
        //}

        public void Quit()
        {
            // Ask the thread to quit itself if it is running
            quit_thyself = true;
            state_change = true;
        }

        public void Pause()
        {
            TheSID.PauseSound();
            have_a_break = true;
            state_change = true;
        }

        public void Resume()
        {
            TheSID.ResumeSound();
            have_a_break = false;
        }

        public void Reset()
        {
            TheCPU.AsyncReset();
            TheCPU1541.AsyncReset();
            TheSID.Reset();
            TheCIA1.Reset();
            TheCIA2.Reset();
            TheIEC.Reset();
        }

        public void NMI()
        {
            TheCPU.AsyncNMI();
        }

        public void VBlank(bool draw_frame)
        {
            TheDisplay.PollKeyboard(TheCIA1.KeyMatrix, TheCIA1.RevMatrix, ref joykey);

            if (TheDisplay.QuitRequested)
			{
                quit_thyself = true;
			}

            // Poll the joysticks.
            TheCIA1.Joystick1 = poll_joystick(0);
            TheCIA1.Joystick2 = poll_joystick(1);

            if (GlobalPrefs.ThePrefs.JoystickSwap)
            {
                byte tmp = TheCIA1.Joystick1;
                TheCIA1.Joystick1 = TheCIA1.Joystick2;
                TheCIA1.Joystick2 = tmp;
            }

            // Joystick keyboard emulation.
            if (TheDisplay.SwapJoysticks)
                TheCIA1.Joystick1 &= joykey;
            else
                TheCIA1.Joystick2 &= joykey;

            // Count TOD clocks.
            TheCIA1.CountTOD();
            TheCIA2.CountTOD();

            // Output a frag.
       //     TheSID.VBlank();

            if (have_a_break)
			{
                return;
			}

            // Update the window if needed.
            frame++;
			
            if (draw_frame)
            {
                // Perform the actual screen update exactly at the
                // beginning of an interval for the smoothest video.
                TheDisplay.Update();

                frameTimer.Stop();

                // Compute the speed index and show it in the speedometer.
                double elapsed_time = (double)frameTimer.ElapsedMicroseconds;
                double speed_index = 20000.0 / elapsed_time * 100.0;

                //System.Diagnostics.Debug.WriteLine(speed_index);

                // Limit speed to 100% if desired
				/*
                if ((speed_index > 100.0) && GlobalPrefs.ThePrefs.LimitSpeed)
                {
                    int sleeptime = (int)((20000 - elapsed_time) / 1000.0);
                    Thread.Sleep(sleeptime);
                    speed_index = 100.0;
                }
                */
				
				emulationSpeed = emulationSpeed * 0.99f + ((float) speed_index) * 0.01f;

                frameTimer.Reset();
                frameTimer.Start();
				
				
            }
        }

        public void NewPrefs(Prefs prefs)
        {
            open_close_joysticks(GlobalPrefs.ThePrefs.Joystick1On, GlobalPrefs.ThePrefs.Joystick2On, prefs.Joystick1On, prefs.Joystick2On);
            patch_kernel(prefs.FastReset, prefs.Emul1541Proc);

            TheDisplay.NewPrefs(prefs);

            TheIEC.NewPrefs(prefs);
            TheJob1541.NewPrefs(prefs);

            TheREU.NewPrefs(prefs);
            TheSID.NewPrefs(prefs);

            // Reset 1541 processor if turned on
            if (!GlobalPrefs.ThePrefs.Emul1541Proc && prefs.Emul1541Proc)
            {
                TheCPU1541.AsyncReset();
            }
        }

        public void patch_kernel(bool fast_reset, bool emul_1541_proc)
        {
            _kernalRom.Patch(fast_reset, emul_1541_proc, orig_kernal_1d84, orig_kernal_1d85);
            _rom1541.Patch();
#if false

            //if (fast_reset)
            //{
            //    Kernal[0x1d84] = 0xa0;
            //    Kernal[0x1d85] = 0x00;
            //}
            //else
            //{
            //    Kernal[0x1d84] = orig_kernal_1d84;
            //    Kernal[0x1d85] = orig_kernal_1d85;
            //}

            //if (emul_1541_proc)
            //{
            //    Kernal[0x0d40] = 0x78;
            //    Kernal[0x0d41] = 0x20;
            //    Kernal[0x0d23] = 0x78;
            //    Kernal[0x0d24] = 0x20;
            //    Kernal[0x0d36] = 0x78;
            //    Kernal[0x0d37] = 0x20;
            //    Kernal[0x0e13] = 0x78;
            //    Kernal[0x0e14] = 0xa9;
            //    Kernal[0x0def] = 0x78;
            //    Kernal[0x0df0] = 0x20;
            //    Kernal[0x0dbe] = 0xad;
            //    Kernal[0x0dbf] = 0x00;
            //    Kernal[0x0dcc] = 0x78;
            //    Kernal[0x0dcd] = 0x20;
            //    Kernal[0x0e03] = 0x20;
            //    Kernal[0x0e04] = 0xbe;
            //}
            //else
            //{
            //    Kernal[0x0d40] = 0xf2;	// IECOut
            //    Kernal[0x0d41] = 0x00;
            //    Kernal[0x0d23] = 0xf2;	// IECOutATN
            //    Kernal[0x0d24] = 0x01;
            //    Kernal[0x0d36] = 0xf2;	// IECOutSec
            //    Kernal[0x0d37] = 0x02;
            //    Kernal[0x0e13] = 0xf2;	// IECIn
            //    Kernal[0x0e14] = 0x03;
            //    Kernal[0x0def] = 0xf2;	// IECSetATN
            //    Kernal[0x0df0] = 0x04;
            //    Kernal[0x0dbe] = 0xf2;	// IECRelATN
            //    Kernal[0x0dbf] = 0x05;
            //    Kernal[0x0dcc] = 0xf2;	// IECTurnaround
            //    Kernal[0x0dcd] = 0x06;
            //    Kernal[0x0e03] = 0xf2;	// IECRelease
            //    Kernal[0x0e04] = 0x07;
            //}

            //// 1541
            //ROM1541[0x2ae4] = 0xea;		// Don't check ROM checksum
            //ROM1541[0x2ae5] = 0xea;
            //ROM1541[0x2ae8] = 0xea;
            //ROM1541[0x2ae9] = 0xea;
            //ROM1541[0x2c9b] = 0xf2;		// DOS idle loop
            //ROM1541[0x2c9c] = 0x00;
            //ROM1541[0x3594] = 0x20;		// Write sector
            //ROM1541[0x3595] = 0xf2;
            //ROM1541[0x3596] = 0xf5;
            //ROM1541[0x3597] = 0xf2;
            //ROM1541[0x3598] = 0x01;
            //ROM1541[0x3b0c] = 0xf2;		// Format track
            //ROM1541[0x3b0d] = 0x02;

#endif
        }
		
		public float EmulationSpeed
		{
			get { return emulationSpeed; }
		}

        public void SaveRAM(string filename)
        {
            throw new NotImplementedException();
        }

        public void SaveSnapshot(string filename)
        {
            throw new NotImplementedException();
        }

        public bool LoadSnapshot(string filename)
        {
            throw new NotImplementedException();
        }

        public int SaveCPUState(Stream f)
        {
            throw new NotImplementedException();
        }

        public int Save1541State(Stream f)
        {
            throw new NotImplementedException();
        }

        public bool Save1541JobState(Stream f)
        {
            throw new NotImplementedException();
        }

        public bool SaveVICState(Stream f)
        {
            throw new NotImplementedException();
        }

        public bool SaveSIDState(Stream f)
        {
            throw new NotImplementedException();
        }

        public bool SaveCIAState(Stream f)
        {
            throw new NotImplementedException();
        }

        public bool LoadCPUState(Stream f)
        {
            throw new NotImplementedException();
        }

        public bool Load1541State(Stream f)
        {
            throw new NotImplementedException();
        }

        public bool Load1541JobState(Stream f)
        {
            throw new NotImplementedException();
        }

        public bool LoadVICState(Stream f)
        {
            throw new NotImplementedException();
        }

        public bool LoadSIDState(Stream f)
        {
            throw new NotImplementedException();
        }

        public bool LoadCIAState(Stream f)
        {
            throw new NotImplementedException();
        }

        #endregion Public methods

        #region Public properties

        public ColorRam Color
        {
            [DebuggerStepThrough]
            get { return _colorRam; }
            [DebuggerStepThrough]
            set { _colorRam = value; }
        }

        public CharacterRom Char
        {
            [DebuggerStepThrough]
            get { return _characterRom; }
            [DebuggerStepThrough]
            set { _characterRom = value; }
        }

        public KernalRom Kernal
        {
            [DebuggerStepThrough]
            get { return _kernalRom; }
            [DebuggerStepThrough]
            set { _kernalRom = value; }
        }

        public BasicRom Basic
        {
            [DebuggerStepThrough]
            get { return _basicRom; }
            [DebuggerStepThrough]
            set { _basicRom = value; }
        }

        public SystemRam RAM
        {
            [DebuggerStepThrough]
            get { return _ram; }
            //[DebuggerStepThrough]
            //set { _ram = value; }
        }

        public Drive1541Ram RAM1541
        {
            [DebuggerStepThrough]
            get { return _ram1541; }
            //[DebuggerStepThrough]
            //set { _RAM1541 = value; }
        }

        public Drive1541Rom ROM1541
        {
            [DebuggerStepThrough]
            get { return _rom1541; }
            [DebuggerStepThrough]
            set { _rom1541 = value; }
        }

        public C64Display TheDisplay
        {
            [DebuggerStepThrough]
            get { return _TheDisplay; }
            [DebuggerStepThrough]
            set { _TheDisplay = value; }
        }

        public MOS6510 TheCPU
        {
            [DebuggerStepThrough]
            get { return _TheCPU; }
            [DebuggerStepThrough]
            set { _TheCPU = value; }
        }

        public MOS6569 TheVIC
        {
            [DebuggerStepThrough]
            get { return _TheVIC; }
            [DebuggerStepThrough]
            set { _TheVIC = value; }
        }

        public MOS6581 TheSID
        {
            [DebuggerStepThrough]
            get { return _TheSID; }
            [DebuggerStepThrough]
            set { _TheSID = value; }
        }

        public MOS6526_1 TheCIA1
        {
            [DebuggerStepThrough]
            get { return _TheCIA1; }
            [DebuggerStepThrough]
            set { _TheCIA1 = value; }
        }

        public MOS6526_2 TheCIA2
        {
            [DebuggerStepThrough]
            get { return _TheCIA2; }
            [DebuggerStepThrough]
            set { _TheCIA2 = value; }
        }

        public IEC TheIEC
        {
            [DebuggerStepThrough]
            get { return _TheIEC; }
            [DebuggerStepThrough]
            set { _TheIEC = value; }
        }

        public REU TheREU
        {
            [DebuggerStepThrough]
            get { return _TheREU; }
            [DebuggerStepThrough]
            set { _TheREU = value; }
        }

        public MOS6502_1541 TheCPU1541
        {
            [DebuggerStepThrough]
            get { return _TheCPU1541; }
            [DebuggerStepThrough]
            set { _TheCPU1541 = value; }
        }

        public Job1541 TheJob1541
        {
            [DebuggerStepThrough]
            get { return _TheJob1541; }
            [DebuggerStepThrough]
            set { _TheJob1541 = value; }
        }

        public UInt32 CycleCounter
        {
            [DebuggerStepThrough]
            get { return _CycleCounter; }
            [DebuggerStepThrough]
            set { _CycleCounter = value; }
        }

        #endregion Public properties

        #region Private methods

        void open_close_joysticks(bool oldjoy1, bool oldjoy2, bool newjoy1, bool newjoy2)
        {
        }

        byte poll_joystick(int port)
        {
            return 0xff;
        }

        void MainLoop()
        {
            Debug.WriteLine("Entering MainLoop");

            thread_running = true;

            while (!quit_thyself)
            {
                if (have_a_break)
                    TheDisplay.WaitUntilActive();

                if (GlobalPrefs.ThePrefs.Emul1541Proc)
                    EmulateCyclesWith1541();
                else
                    EmulateCyclesWithout1541();

                state_change = false;
            }

            thread_running = false;

            Debug.WriteLine("Exiting MainLoop");
        }




        void EmulateCyclesWith1541()
        {
            thread_running = true;

            while (!quit_thyself)
            {
                // The order of calls is important here
                if (TheVIC.EmulateCycle())
                    TheSID.EmulateLine();

                TheCIA1.CheckIRQs();
                TheCIA2.CheckIRQs();
                TheCIA1.EmulateCycle();
                TheCIA2.EmulateCycle();
                TheCPU.EmulateCycle();

                TheCPU1541.CountVIATimers(1);

                if (!TheCPU1541.Idle)
                    TheCPU1541.EmulateCycle();

                CycleCounter++;
            }
        }

        void EmulateCyclesWithout1541()
        {
#if TIMERS
            uint lc = CycleCounter;
            HiResTimer timer = new HiResTimer();
            timer.Start();
            const uint cycleCount = 4000000;
#endif

            thread_running = true;
            while (!quit_thyself)
            {

                // The order of calls is important here
                if (TheVIC.EmulateCycle())
                    TheSID.EmulateLine();
                TheCIA1.CheckIRQs();
                TheCIA2.CheckIRQs();
                TheCIA1.EmulateCycle();
                TheCIA2.EmulateCycle();
                TheCPU.EmulateCycle();

                CycleCounter++;

#if TIMERS
                if (CycleCounter - lc == cycleCount)
                {
                    timer.Stop();
                    lc = CycleCounter;
                    double elapsedSec = timer.ElapsedMilliseconds / 1000.0f;

                    //Debug.WriteLine("------------------------------------");
                    //Debug.WriteLine(string.Format("{0} ms elapsed for {1:N} cycles", timer.ElapsedMilliseconds, cycleCount));
                    //Debug.WriteLine(string.Format("CIA1: TA Interrupts: {0} -> int/s: {1}", TheCIA1.ta_interrupts, TheCIA1.ta_interrupts / elapsedSec));
                    //Debug.WriteLine(string.Format("CPU Instructions: {0} -> ins/s: {1}", TheCPU.ins_counter, TheCPU.ins_counter / elapsedSec));
                    
                    // reset counters
                    TheCIA1.ta_interrupts = 0;
                    TheCIA1.tb_interrupts = 0;
                    TheCIA2.ta_interrupts = 0;
                    TheCIA2.tb_interrupts = 0;
                    TheCPU.ins_counter = 0;

                    timer.Reset();
                    timer.Start();
                    //TheDisplay.Surface.Update();
                }
#endif
            }
        }

        #endregion Private methods
    }
}
