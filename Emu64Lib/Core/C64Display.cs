#define NSCANLINE

using System;
using System.Diagnostics;

namespace C64Lib.Core
{
    public interface RenderListener
    {
        void updateFrame(byte[] pixels, int width, int height, int bitsPerPixel);
    }

    public class C64Display
    {
        //#define DISPLAY_4BIT_FORMAT

        #region Public constants
        public const int DISPLAY_X = 0x180;
        public const int DISPLAY_Y = 0x110;
        #if (DISPLAY_4BIT_FORMAT)
            public const int DISPLAY_BPP = 4;
        #else
            public const int DISPLAY_BPP = 8;
        #endif
        #endregion

        #region Private Constants
        const byte joystate = 0xff;
        #endregion

        byte[] _pixels = new byte[DISPLAY_X * DISPLAY_Y * DISPLAY_BPP / 8];
        EmulatorIOAdapter ioAdapter;

        byte[][] _indexLines;

        #region Public methods


        //public MediaStreamSource Video
        //{
        //    get { return _video; }
        //    set { _video = value; }
        //}


        public C64Display(C64 c64, EmulatorIOAdapter ioAdapter)
        {
            _TheC64 = c64;

            this.ioAdapter = ioAdapter;

            //CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);

            //_ui.ScreenImage.Source = _image;

            _indexLines = new byte[16][];

            for (int i = 0; i < 16; i++)
            {
                _indexLines[i] = new byte[DISPLAY_X];
                for (int p = 0; p < DISPLAY_X; p++)
                {
                    _indexLines[i][p] = (byte) i;
                }
            }
        }

        public int getWidth()
        {
            return DISPLAY_X;
        }

        public int getHeight()
        {
            return DISPLAY_Y;
        }

        public int getBitsPerPixel()
        {
            return DISPLAY_BPP;
        }

        public Color[] getPalette()
        {
            return _colorPalette;
        }

        //private void ImageStartRender()
        //{
        //    if (_imageLocked) return;

        //    _image.Dispatcher.BeginInvoke(new Action(delegate() 
        //        {
        //            _image.Lock();
        //            _imageLocked = true;
        //        }));
        //}

        //private void ImageEndRender()
        //{
        //    if (_imageLocked)
        //    {
        //        _image.Dispatcher.BeginInvoke(new Action(delegate()
        //        {
        //            _image.Invalidate();
        //            _image.Unlock();

        //            _imageLocked = false;
        //        }));

        //    }
        //    else
        //    {
        //        // TODO
        //    }
        //}



        //private void SetPixel(int index, int color)
        //{
        //    //_image.Lock();
            
        //    _image[index] = color;
            
        //    //_image.Unlock();
        //}

        public void SetPixels(int startIndex, int count, byte paletteIndex)
        {

#if (DISPLAY_4BIT_FORMAT)
                int ofs = startIndex;

                if (0 != (ofs & 0x1))
                {
                    // set/keep: sssskkkk
                    _pixels[ofs] = (byte)((_pixels[ofs] & 0xf) | paletteIndex);
                    ofs++;
                    count--;
                }

                int ops = count / 2;
                byte twoPixels = (byte)((paletteIndex << 4) | paletteIndex);

                while (count >= 2)
                {
                    _pixels[ofs++] = twoPixels;
                    count -= 2;
                }

                if (count > 0)
                {
                    // set/keep: kkkkssss
                    _pixels[ofs] = (byte)((_pixels[ofs] & 0x0f) | (paletteIndex << 4));
                    ofs++;
                    count--;
                }
#else

            // memset

            Buffer.BlockCopy(_indexLines[paletteIndex], 0, _pixels, startIndex, count);

            /*
            while (count-- > 0)
            {
                _pixels[startIndex + count] = paletteIndex;
            }
            */

#endif
        }

        public void SetPixel(int index, byte paletteIndex)
        {
            #if (DISPLAY_4BIT_FORMAT)
                if (0 != (index & 0x1))
                {
                    // set/keep: sssskkkk
                    _pixels[index] = (byte)((_pixels[index] & 0x0f) | (paletteIndex << 4));
                }
                else
                {
                    // set/keep: kkkkssss
                    _pixels[index] = (byte)((_pixels[index] & 0xf0) | paletteIndex);
                }
            #else
                _pixels[index] = paletteIndex;
            #endif
        }

        public void SetPixelsMulti(int p, byte[] colorData, byte colorMask)
        {
            SetPixel(p + 7, colorData[colorMask & 3]);
            SetPixel(p + 6, colorData[colorMask & 3]);
            colorMask >>= 2;

            SetPixel(p + 5, colorData[colorMask & 3]);
            SetPixel(p + 4, colorData[colorMask & 3]);
            colorMask >>= 2;

            SetPixel(p + 3, colorData[colorMask & 3]);
            SetPixel(p + 2, colorData[colorMask & 3]);
            colorMask >>= 2;

            SetPixel(p + 1, colorData[colorMask & 3]);
            SetPixel(p + 0, colorData[colorMask & 3]);
        }

        public void SetPixelsStd(int p, byte[] colorData, byte colorMask)
        {
			/*
            SetPixel(p + 7, colorData[colorMask & 1]); colorMask >>= 1;
            SetPixel(p + 6, colorData[colorMask & 1]); colorMask >>= 1;
            SetPixel(p + 5, colorData[colorMask & 1]); colorMask >>= 1;
            SetPixel(p + 4, colorData[colorMask & 1]); colorMask >>= 1;
            SetPixel(p + 3, colorData[colorMask & 1]); colorMask >>= 1;
            SetPixel(p + 2, colorData[colorMask & 1]); colorMask >>= 1;
            SetPixel(p + 1, colorData[colorMask & 1]); colorMask >>= 1;
            SetPixel(p + 0, colorData[colorMask]);
            */
			
			int ptr = p+7;
			
			_pixels[ptr--] = colorData[colorMask & 1]; colorMask >>= 1;
			_pixels[ptr--] = colorData[colorMask & 1]; colorMask >>= 1;
			_pixels[ptr--] = colorData[colorMask & 1]; colorMask >>= 1;
			_pixels[ptr--] = colorData[colorMask & 1]; colorMask >>= 1;
			_pixels[ptr--] = colorData[colorMask & 1]; colorMask >>= 1;
			_pixels[ptr--] = colorData[colorMask & 1]; colorMask >>= 1;
			_pixels[ptr--] = colorData[colorMask & 1]; colorMask >>= 1;
			_pixels[ptr]   = colorData[colorMask & 1];
        }

        internal void Update()
        {
            ioAdapter.onNewFrame(_pixels);

#if false
            //// Draw speedometer/LEDs
            //Rectangle r = new Rectangle(0, DISPLAY_Y, DISPLAY_X, 15);
            //_c64Screen.DrawFilledBox(r, Color.Gray);

            //r.Width = DISPLAY_X; r.Height = 1;
            //_c64Screen.DrawFilledBox(r, Color.LightGray);

            //r.Y = DISPLAY_Y + 14;
            //_c64Screen.DrawFilledBox(r, Color.DarkGray);
            //r.Width = 16;

            //for (int i = 2; i < 6; i++)
            //{
            //    r.X = DISPLAY_X * i / 5 - 24; r.Y = DISPLAY_Y + 4;
            //    _c64Screen.DrawFilledBox(r, Color.DarkGray);
            //    r.Y = DISPLAY_Y + 10;
            //    _c64Screen.DrawFilledBox(r, Color.LightGray);
            //}

            //r.Y = DISPLAY_Y; r.Width = 1; r.Height = 15;
            //for (int i = 0; i < 5; i++)
            //{
            //    r.X = DISPLAY_X * i / 5;
            //    _c64Screen.DrawFilledBox(r, Color.LightGray);
            //    r.X = DISPLAY_X * (i + 1) / 5 - 1;
            //    _c64Screen.DrawFilledBox(r, Color.DarkGray);
            //}

            //r.Y = DISPLAY_Y + 4; r.Height = 7;
            //for (int i = 2; i < 6; i++)
            //{
            //    r.X = DISPLAY_X * i / 5 - 24;
            //    _c64Screen.DrawFilledBox(r, Color.DarkGray);
            //    r.X = DISPLAY_X * i / 5 - 9;
            //    _c64Screen.DrawFilledBox(r, Color.LightGray);
            //}
            //r.Y = DISPLAY_Y + 5; r.Width = 14; r.Height = 5;
            //for (int i = 0; i < 4; i++)
            //{
            //    r.X = DISPLAY_X * (i + 2) / 5 - 23;
            //    Color c;
            //    switch (led_state[i])
            //    {
            //        case DriveLEDState.DRVLED_ON:
            //            c = Color.Green;
            //            break;
            //        case DriveLEDState.DRVLED_ERROR:
            //            c = Color.Red;
            //            break;
            //        default:
            //            c = Color.Black;
            //            break;
            //    }
            //    _c64Screen.DrawFilledBox(r, c);
            //}

            //draw_string(DISPLAY_X * 1 / 5 + 8, DISPLAY_Y + 4, "D\x12 8", GetPaletteColorIndex(Color.Black), GetPaletteColorIndex(Color.Gray));
            //draw_string(DISPLAY_X * 2 / 5 + 8, DISPLAY_Y + 4, "D\x12 9", GetPaletteColorIndex(Color.Black), GetPaletteColorIndex(Color.Gray));
            //draw_string(DISPLAY_X * 3 / 5 + 8, DISPLAY_Y + 4, "D\x12 10", GetPaletteColorIndex(Color.Black), GetPaletteColorIndex(Color.Gray));
            //draw_string(DISPLAY_X * 4 / 5 + 8, DISPLAY_Y + 4, "D\x12 11", GetPaletteColorIndex(Color.Black), GetPaletteColorIndex(Color.Gray));
            //draw_string(16, DISPLAY_Y + 4, speedometer_string, GetPaletteColorIndex(Color.Black), GetPaletteColorIndex(Color.Gray));
#endif

#if SCANLINE
            unsafe
            {
                byte* srcscanline = (byte*)_c64Screen.Pixels;
                byte* destscanline = (byte*)_videoDisplay.Pixels;

                short srcstride = _c64Screen.Pitch;
                short deststride = (short)(_videoDisplay.Pitch * 2);

                for (int y = 0; y < DISPLAY_Y + 17; y++)
                {
                    for (int x = 0; x < DISPLAY_X; x++)
                    {
                        destscanline[x*2] = srcscanline[x];
                        destscanline[x*2+1] = srcscanline[x];
                    }

                    srcscanline += srcstride;
                    destscanline += deststride;
                }
            }
            _videoDisplay.Flip();
#else


            // TheC64.Video.Flip();



            //_dispatcher.Dispatcher.BeginInvoke(new Action(delegate()
            //{
            //    _image.Lock();

            //    byte[] colorParts = new byte[4];

            //    for (int i = 0; i < DISPLAY_X * DISPLAY_Y; i++)
            //    {
            //        colorParts[0] = _pixels[i].B;
            //        colorParts[1] = _pixels[i].G;
            //        colorParts[2] = _pixels[i].R;
            //        colorParts[3] = 0;

            //        _image[i] = BitConverter.ToInt32(colorParts, 0);
            //    }

            //    _image.Invalidate();
            //    _image.Unlock();

            //}));



            //if (_tempFrameCounter == long.MaxValue)
            //    _tempFrameCounter = 0;
            //else
            //    _tempFrameCounter++;
           
#endif
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            //_dispatcher.Dispatcher.BeginInvoke(new Action(delegate()
            //{
            //    _image.Lock();

            //    byte[] colorParts = new byte[4];

            //    for (int i = 0; i < DISPLAY_X * DISPLAY_Y; i++)
            //    {
            //        colorParts[0] = _pixels[i].B;
            //        colorParts[1] = _pixels[i].G;
            //        colorParts[2] = _pixels[i].R;
            //        colorParts[3] = 0;

            //        _image[i] = BitConverter.ToInt32(colorParts, 0);
            //    }

            //    _image.Invalidate();
            //    _image.Unlock();

            //    _ui.ScreenImage.Source = _image;

            //}));

        }

        //static long _tempFrameCounter = 0;






        public void UpdateLEDs(DriveLEDState l0, DriveLEDState l1, DriveLEDState l2, DriveLEDState l3)
        {
            led_state[0] = l0;
            led_state[1] = l1;
            led_state[2] = l2;
            led_state[3] = l3;
        }


        //long speedometerCount = 0, speedometerTally = 0;
        //const int speedoLogCapacity = 1000; System.Collections.Generic.Queue<int> speedoLog = new System.Collections.Generic.Queue<int>(speedoLogCapacity);
        internal void Speedometer(int speed)
        {
            //speedometerCount++; speedometerTally += speed; speed = (int)((speedometerTally + (speedometerCount >> 1)) / speedometerCount);
            //speedoLog.Enqueue(speed); speed = (int)(System.Linq.Enumerable.Average(speedoLog) + 0.5); if (speedoLog.Count == speedoLogCapacity) speedoLog.Dequeue();
            speedometer_string = String.Format("{0,4}%", speed);
        }

        // TODO
        //unsafe internal byte* BitmapBase
        //{
        //    get
        //    {
        //        return (byte*)_c64Screen.Pixels;
        //    }
        //}

        public int BitmapXMod
        {
            get
            {
                // PMB
                //return (int)_c64Screen.Pitch;

                // this would need to be changed if we (eg) double-size and don't want SL to handle the scaling
                return DISPLAY_X;
            }
        }

        public void PollKeyboard(byte[] key_matrix, byte[] rev_matrix, ref byte joystick)
        {
            int[] keyboardEvents = ioAdapter.getKeyboardEvents();
            if (null == keyboardEvents)
            {
                return;
            }

            foreach (int keyEvent in keyboardEvents)
            {
                int keyFlags = (keyEvent & (int)KeyCode.KEYFLAG_MASK);
                int key = (keyEvent & (int)KeyCode.KEYCODE_MASK);

                if (0 != (keyFlags & (int)KeyCode.KEYFLAG_COMMAND))
                {
                    int command = (keyEvent & 0xff) | (int)KeyCode.KEYFLAG_COMMAND;

                    switch (command)
                    {
                        case (int)KeyCode.COMMAND_DEBUGGER_TOGGLE:
                            //SAM(TheC64);
#if DEBUG_INSTRUCTIONS
                                TheC64.TheCPU.debugLogger.Enabled = !TheC64.TheCPU.debugLogger.Enabled;
#endif
                            break;

                        //case Key.F10:	// F10: Quit
                        //    quit_requested = true;
                        //    break;

                        case (int)KeyCode.COMMAND_RESTORE: // F11: NMI (Restore). F11 is taken by the browser for full-screen. Run out of browser to use this
                            _TheC64.NMI();
                            break;

                        case (int)KeyCode.COMMAND_RESET: // F12: Reset
                            TheC64.Reset();
                            break;

                        case (int)KeyCode.COMMAND_SWAP_JOYSTICKS:
                            swapjoysticks = swapjoysticks ? false : true;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    bool keyUp = (0 != (keyFlags & (int)KeyCode.KEYFLAG_RELEASED));
                    processKey(key, keyUp, key_matrix, rev_matrix, ref joystick);
                }

                /*
                else if (keyEvent < 0)
                {
                    int key = -keyEvent;
                    if (key == (int)Key.CapsLock)
                        swapjoysticks = false;
                    else

                }
                 */
            }

            /*

            foreach (KeyboardEvent evt in events)
            {
                switch (evt.EventType)
                {
                    // Key pressed
                    case KeyboardEventType.KeyDown:
                        switch (evt.Key)
                        {

                            case Key.F9:	// F9: Invoke SAM
                                //SAM(TheC64);
#if DEBUG_INSTRUCTIONS
                                TheC64.TheCPU.debugLogger.Enabled = !TheC64.TheCPU.debugLogger.Enabled;
#endif
                                break;

                            //case Key.F10:	// F10: Quit
                            //    quit_requested = true;
                            //    break;

                            case Key.F11:	// F11: NMI (Restore). F11 is taken by the browser for full-screen. Run out of browser to use this
                                _TheC64.NMI();
                                break;

                            case Key.F12:	// F12: Reset
                                TheC64.Reset();
                                break;

                            case Key.CapsLock:
                                swapjoysticks = true;
                                break;

                            // TODO
                            //case Key.KeypadPlus:	// '+' on keypad: Increase SkipFrames
                            //    GlobalPrefs.ThePrefs.SkipFrames++;
                            //    break;

                            //case Key.KeypadMinus:	// '-' on keypad: Decrease SkipFrames
                            //    if (GlobalPrefs.ThePrefs.SkipFrames > 1)
                            //        GlobalPrefs.ThePrefs.SkipFrames--;
                            //    break;

                            //case Key.KeypadMultiply:	// '*' on keypad: Toggle speed limiter
                            //    GlobalPrefs.ThePrefs.LimitSpeed = !GlobalPrefs.ThePrefs.LimitSpeed;
                            //    break;

                            default:
                                translate_key(evt.Key, evt.PlatformKeyCode, false, key_matrix, rev_matrix, ref joystick);
                                break;
                        }
                        break;

                    // Key released
                    case KeyboardEventType.KeyUp:
                        //keyEvent = (KeyboardEventArgs)evt;
                        if (evt.Key == Key.CapsLock)
                            swapjoysticks = false;
                        else
                            translate_key(evt.Key, evt.PlatformKeyCode, true, key_matrix, rev_matrix, ref joystick);
                        break;

                    // TODO
                    //// Quit Frodo
                    //case EventTypes.Quit:
                    //    quit_requested = true;
                    //    break;
                }
            }
            */
        }

        private static int MATRIX(int a, int b)
        {
            return (((a) << 3) | (b));
        }

        void processKey(int c64_key, bool key_up, byte[] key_matrix, byte[] rev_matrix, ref byte joystick)
        {
            if (c64_key < 0)
                return;

            // Handle joystick emulation
            if ((c64_key & 0x40) != 0)
            {
                c64_key &= 0x1f;
                if (key_up)
                    joystick |= (byte)c64_key;
                else
                    joystick &= (byte)~c64_key;
                return;
            }

            // Handle other keys
            bool shifted = (c64_key & 0x80) != 0;
            int c64_byte = (c64_key >> 3) & 7;
            int c64_bit = c64_key & 7;
            if (key_up)
            {
                if (shifted)
                {
                    key_matrix[6] |= 0x10;
                    rev_matrix[4] |= 0x40;
                }
                key_matrix[c64_byte] |= (byte)(1 << c64_bit);
                rev_matrix[c64_bit] |= (byte)(1 << c64_byte);
            }
            else
            {
                if (shifted)
                {
                    key_matrix[6] &= 0xef;
                    rev_matrix[4] &= 0xbf;
                }
                key_matrix[c64_byte] &= (byte)~(1 << c64_bit);
                rev_matrix[c64_bit] &= (byte)~(1 << c64_byte);
            }
        }

        public void InitColors(byte[] colors)
        {
            //Tao.Sdl.Sdl.SDL_Color[] palette = new Tao.Sdl.Sdl.SDL_Color[21];
            //for (int i = 0; i < 16; i++)
            //{
            //    palette[i].r = palette_red[i];
            //    palette[i].g = palette_green[i];
            //    palette[i].b = palette_blue[i];
            //}

            //IntPtr current = Tao.Sdl.Sdl.SDL_GetVideoSurface();
            //Tao.Sdl.Sdl.SDL_SetColors(current, palette, 0, 21);

            for (int i = 0; i < 256; i++)
            {
                colors[i] = (byte)(i & 0x0f);
            }
        }

        internal void NewPrefs(Prefs prefs)
        {
            //throw new Exception("The method or operation is not implemented.");
        }

        internal void WaitUntilActive()
        {

        }

        internal void Initialize()
        {
            init_graphics();

        }

        internal void ShowRequester(string a, string button1, string button2)
        {
            ShowRequester(a, button1);
        }

        internal void ShowRequester(string a, string button1)
        {
            Debug.WriteLine(string.Format("{0}: {1}", a, button1));
        }

        #endregion Public methods

        #region Public properties

        internal bool SwapJoysticks
        {
            [DebuggerStepThrough]
            get { return swapjoysticks; }
            [DebuggerStepThrough]
            set { swapjoysticks = value; }
        }

        public C64 TheC64
        {
            [DebuggerStepThrough]
            get { return _TheC64; }
            [DebuggerStepThrough]
            set { _TheC64 = value; }
        }

        public bool QuitRequested
        {
            [DebuggerStepThrough]
            get { return quit_requested; }
            [DebuggerStepThrough]
            set { quit_requested = value; }
        }

        // TODO
        //public Surface C64Screen
        //{
        //    get { return _c64Screen; }
        //}

        #endregion

        #region Private methods

        // TODO
        //Surface _c64Screen;
#if SCANLINE
        Surface _videoDisplay;
#endif
        int init_graphics()
        {
            // TODO
//            // Init SDL
//            Video.Initialize();

//#if SCANLINE
//            _c64Screen = Video.CreateRgbSurface(DISPLAY_X, DISPLAY_Y + 17, 8, 0xff, 0xff, 0xff, 0x00, false);
//            _videoDisplay = Video.SetVideoModeWindow(DISPLAY_X * 2, (DISPLAY_Y + 17) * 2, 8);
//#else
//            _c64Screen = Video.SetVideoModeWindow(DISPLAY_X, DISPLAY_Y + 17, 8);
//#endif

//            // Open window
//            Video.WindowCaption = "Sharp-C64";


            return 1;
        }

        // TODO
        //private static readonly Color[] palette_color_names = new Color[16]
        //{
        //    Colors.Black,
        //    Colors.White,
        //    Colors.Red,
        //    Colors.Cyan,
        //    Colors.Purple,
        //    Colors.Green,
        //    Colors.Blue,
        //    Colors.Yellow,
        //    Colors.Orange,
        //    Colors.Brown,
        //    Color.FromArgb(0xFF, 0xFF, 0xC0, 0xCB),  // "light red" / Pink
        //    Colors.DarkGray,
        //    Colors.Gray,
        //    Color.FromArgb(0xFF, 0x90, 0xEE, 0x90), //Colors.LightGreen,
        //    Color.FromArgb(0xFF, 0xAD, 0xD8, 0xE6), //Colors.LightBlue,
        //    Colors.LightGray
        //};

        // TODO
        //internal static byte GetPaletteColorIndex(Color named_palette_color)
        //{
        //    for (byte index = 0; index < palette_color_names.Length; index++)
        //    {
        //        if (named_palette_color == palette_color_names[index])
        //        {
        //            return index;
        //        }
        //    }
        //    throw new NotSupportedException(string.Format(
        //        "Invalid named color: {0}", named_palette_color));
        //}

        // TODO
        //unsafe private void draw_string(int x, int y, string str, byte front_color, byte back_color)
        //{
        //    byte* pb = (byte*)_c64Screen.Pixels + _c64Screen.Pitch * y + x;
        //    char c;
        //    fixed (byte* qq = TheC64.Char)
        //    {
        //        for (int i = 0; i < str.Length; i++)
        //        {
        //            c = str[i];
        //            byte* q = qq + c * 8 + 0x800;
        //            byte* p = pb;
        //            for (int j = 0; j < 8; j++)
        //            {
        //                byte v = *q++;
        //                p[0] = (v & 0x80) != 0 ? front_color : back_color;
        //                p[1] = (v & 0x40) != 0 ? front_color : back_color;
        //                p[2] = (v & 0x20) != 0 ? front_color : back_color;
        //                p[3] = (v & 0x10) != 0 ? front_color : back_color;
        //                p[4] = (v & 0x08) != 0 ? front_color : back_color;
        //                p[5] = (v & 0x04) != 0 ? front_color : back_color;
        //                p[6] = (v & 0x02) != 0 ? front_color : back_color;
        //                p[7] = (v & 0x01) != 0 ? front_color : back_color;
        //                p += _c64Screen.Pitch;
        //            }
        //            pb += 8;
        //        }
        //    }
        //}

        #endregion

        #region Private fields

        C64 _TheC64;

        DriveLEDState[] led_state = new DriveLEDState[4];
        DriveLEDState[] old_led_state = new DriveLEDState[4];

        bool swapjoysticks;

        string speedometer_string = String.Empty;

        bool quit_requested = false;

        #endregion Private fields

#if USE_THEORETICAL_COLORS

        // C64 color palette (theoretical values)
        static readonly byte[] palette_red = {
	        0x00, 0xff, 0xff, 0x00, 0xff, 0x00, 0x00, 0xff, 0xff, 0x80, 0xff, 0x40, 0x80, 0x80, 0x80, 0xc0
        };

        static readonly byte[] palette_green = {
	        0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x80, 0x40, 0x80, 0x40, 0x80, 0xff, 0x80, 0xc0
        };

        static readonly byte[] palette_blue = {
	        0x00, 0xff, 0x00, 0xff, 0xff, 0x00, 0xff, 0x00, 0x00, 0x00, 0x80, 0x40, 0x80, 0x80, 0xff, 0xc0
        };

#else

        static readonly Color[] _colorPalette =
        {
            Color.FromArgb(0xFF, 0x00, 0x00, 0x00),
            Color.FromArgb(0xFF, 0xff, 0xff, 0xff),
            Color.FromArgb(0xFF, 0x99, 0x00, 0x00),
            Color.FromArgb(0xFF, 0x00, 0xff, 0xcc),
            Color.FromArgb(0xFF, 0xcc, 0x00, 0xcc),
            Color.FromArgb(0xFF, 0x44, 0xcc, 0x44),
            Color.FromArgb(0xFF, 0x11, 0x00, 0x99),
            Color.FromArgb(0xFF, 0xff, 0xff, 0x00),
            Color.FromArgb(0xFF, 0xaa, 0x55, 0x00),
            Color.FromArgb(0xFF, 0x66, 0x33, 0x00),
            Color.FromArgb(0xFF, 0xff, 0x66, 0x66),
            Color.FromArgb(0xFF, 0x40, 0x40, 0x40),
            Color.FromArgb(0xFF, 0x80, 0x80, 0x80),
            Color.FromArgb(0xFF, 0x66, 0xff, 0x66),
            Color.FromArgb(0xFF, 0x77, 0x77, 0xff),
            Color.FromArgb(0xFF, 0xc0, 0xc0, 0xc0)
        };

        //// TODO, get rid of conversion here
        //static readonly int[] _colorPalette =
        //{
        //    Color.FromArgb(0xFF, 0x00, 0x00, 0x00).ToBrg32(),
        //    Color.FromArgb(0xFF, 0xff, 0xff, 0xff).ToBrg32(),
        //    Color.FromArgb(0xFF, 0x99, 0x00, 0x00).ToBrg32(),
        //    Color.FromArgb(0xFF, 0x00, 0xff, 0xcc).ToBrg32(),
        //    Color.FromArgb(0xFF, 0xcc, 0x00, 0xcc).ToBrg32(),
        //    Color.FromArgb(0xFF, 0x44, 0xcc, 0x44).ToBrg32(),
        //    Color.FromArgb(0xFF, 0x11, 0x00, 0x99).ToBrg32(),
        //    Color.FromArgb(0xFF, 0xff, 0xff, 0x00).ToBrg32(),
        //    Color.FromArgb(0xFF, 0xaa, 0x55, 0x00).ToBrg32(),
        //    Color.FromArgb(0xFF, 0x66, 0x33, 0x00).ToBrg32(),
        //    Color.FromArgb(0xFF, 0xff, 0x66, 0x66).ToBrg32(),
        //    Color.FromArgb(0xFF, 0x40, 0x40, 0x40).ToBrg32(),
        //    Color.FromArgb(0xFF, 0x80, 0x80, 0x80).ToBrg32(),
        //    Color.FromArgb(0xFF, 0x66, 0xff, 0x66).ToBrg32(),
        //    Color.FromArgb(0xFF, 0x77, 0x77, 0xff).ToBrg32(),
        //    Color.FromArgb(0xFF, 0xc0, 0xc0, 0xc0).ToBrg32()
        //};





#endif

    }


    
}
