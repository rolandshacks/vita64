#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.GamerServices;
using C64Lib;
#endregion

namespace C64Emu
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class EmulatorUI : Game, EmulatorIOAdapter
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private EmulatorApplication emu;
        private FontRenderer font;

        private byte[] pixelBuffer;
        private Texture2D textureBuffer;
        private Color[] colorBuffer;
        private Color[] palette;
        private bool pixelBufferChanged;
        private List<int> keyEvents = new List<int>();

        private KeyboardState lastKeyboardState;
        private KeyboardState keyboardState;

        private GamePadState lastGamePadState;
        private GamePadState gamePadState;

        protected class TextAlignment
        {
            public const int LEFT = 0x1;
            public const int CENTER = 0x2;
            public const int RIGHT = 0x4;
            public const int TOP = 0x10;
            public const int MIDDLE = 0x20;
            public const int BOTTOM = 0x40;
        };

        public class FontRenderer
        {
            public FontRenderer(FontFile fontFile, Texture2D fontTexture)
            {
                _fontFile = fontFile;
                _texture = fontTexture;
                _characterMap = new Dictionary<char, FontChar>();

                foreach (var fontCharacter in _fontFile.Chars)
                {
                    char c = (char)fontCharacter.ID;
                    _characterMap.Add(c, fontCharacter);
                }
            }

            private Dictionary<char, FontChar> _characterMap;
            private FontFile _fontFile;
            private Texture2D _texture;

            public Point MeasureText(string text)
            {
                int w = 0;
                int h = 0;

                foreach (char c in text)
                {
                    FontChar fc;
                    if (_characterMap.TryGetValue(c, out fc))
                    {
                        if (fc.Height > h) h = fc.Height;
                        w += fc.Width;
                    }
                }

                return new Point(w, h);
            }

            public void DrawText(SpriteBatch spriteBatch, int x, int y, string text, uint argb, int alignment)
            {
                if (alignment != 0)
                {
                    Point textSize = MeasureText(text);

                    if (0 != (alignment & TextAlignment.CENTER)) x -= textSize.X / 2;
                    if (0 != (alignment & TextAlignment.RIGHT))  x -= textSize.X;

                    if (0 != (alignment & TextAlignment.MIDDLE)) y -= textSize.Y / 2;
                    if (0 != (alignment & TextAlignment.BOTTOM)) y -= textSize.Y;
                }

                Color col = new Color((byte)(argb >> 16), (byte)(argb >> 8), (byte)(argb >> 0), (byte)(argb >> 24));

                //Color col = Color.White; // argb

                int dx = x;
                int dy = y;
                foreach (char c in text)
                {
                    FontChar fc;
                    if (_characterMap.TryGetValue(c, out fc))
                    {
                        var sourceRectangle = new Rectangle(fc.X, fc.Y, fc.Width, fc.Height);
                        var position = new Vector2(dx + fc.XOffset, dy + fc.YOffset);

                        spriteBatch.Draw(_texture, position, sourceRectangle, col); 
                        dx += fc.XAdvance;
                    }
                }
            }
        }

        protected Statistics statistics;

        private bool doFiltering = false;

        public EmulatorUI()
            : base()
        {
            graphics = new GraphicsDeviceManager(this);

            emu = new EmulatorApplication(this);

            int scaleFactor = 2;
            graphics.PreferredBackBufferWidth = emu.Video.width * scaleFactor;
            graphics.PreferredBackBufferHeight = emu.Video.height * scaleFactor;


            Content.RootDirectory = "Content";
        }

        protected void startup()
        {
            statistics = new Statistics();
        }

        protected void shutdown()
        {
        }

        protected virtual void drawOverlay()
        {
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();

            emu.initialize();

            pixelBuffer = new byte[emu.Video.size];
            pixelBufferChanged = false;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            textureBuffer = new Texture2D(GraphicsDevice, emu.Video.width, emu.Video.height);

            palette = new Color[emu.Video.palette.Length];
            for (int i = 0; i < palette.Length; i++)
            {
                palette[i].A = 0xff;
                palette[i].R = emu.Video.palette[i].R;
                palette[i].G = emu.Video.palette[i].G;
                palette[i].B = emu.Video.palette[i].B;
            }

            Rectangle rect = new Rectangle(0, 0, emu.Video.width, emu.Video.height);
            colorBuffer = new Color[rect.Width * rect.Height];
            //textureBuffer.GetData<Color>(0, rect, colorBuffer, 0, colorBuffer.Length);

            var fontFilePath = System.IO.Path.Combine(Content.RootDirectory, "arial_bitmapfont.fnt");
            var fontFile = FontLoader.Load(fontFilePath);
            var fontTexture = Content.Load<Texture2D>("arial_bitmapfont_0.png");

            font = new FontRenderer(fontFile, fontTexture);

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            lastKeyboardState = keyboardState;
            keyboardState = Keyboard.GetState();
            storeKeyboardEvents();

            lastGamePadState = gamePadState;
            gamePadState = GamePad.GetState(PlayerIndex.One);

            if (gamePadState.Buttons.Back == ButtonState.Pressed ||
                (keyboardState.IsKeyUp(Keys.F12) && lastKeyboardState.IsKeyDown(Keys.F12)))
            {
                Exit();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            updateTextureBuffer();

            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, (doFiltering ? SamplerState.LinearClamp : SamplerState.PointClamp), DepthStencilState.Default, RasterizerState.CullNone);

            int screenWidth = GraphicsDevice.Viewport.Width;
            int screenHeight = GraphicsDevice.Viewport.Height;

            int scale = Math.Min(screenWidth / emu.Video.width,
                                 screenHeight / emu.Video.height);

            int width = scale * emu.Video.width;
            int height = scale * emu.Video.height;

            Rectangle dest = new Rectangle((screenWidth - width) / 2, (screenHeight - height) / 2, width, height);

            spriteBatch.Draw(textureBuffer, dest, Color.White);

            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, (doFiltering ? SamplerState.LinearClamp : SamplerState.PointClamp), DepthStencilState.Default, RasterizerState.CullNone);

            drawOverlay();


            spriteBatch.End();

            base.Draw(gameTime);
        }

        protected void drawText(string text, float x, float y, uint argb, int alignment=0)
        {
            font.DrawText(spriteBatch, (int)x, (int)y, text, argb, alignment);
        }

        public void onNewFrame(byte[] frameBuffer)
        {
            if (false == pixelBufferChanged)
            {
                int frameBufferSize = pixelBuffer.Length;

                Buffer.BlockCopy(frameBuffer, 0, pixelBuffer, 0, pixelBuffer.Length);

                for (int i = 0; i < frameBufferSize; i++)
                {
                    byte pixel = pixelBuffer[i];
                    colorBuffer[i] = palette[pixel];
                }

                pixelBufferChanged = true;
            }
        }

        private void storeKeyboardEvents()
        {
            Keys[] lastPressedKeys = lastKeyboardState.GetPressedKeys();
            Keys[] pressedKeys = keyboardState.GetPressedKeys();

            foreach (Keys k in lastPressedKeys)
            {
                if (keyboardState.IsKeyUp(k))
                {
                    int keyCode = TranslateKeyCode(k);
                    //Console.Out.WriteLine("KEY: " + k + " / " + keyCode);
                    if (keyCode >= 0)
                    {
                        keyEvents.Add(keyCode | (int) KeyCode.KEYFLAG_RELEASED);
                    }
                }
            }

            foreach (Keys k in pressedKeys)
            {
                if (lastKeyboardState.IsKeyUp(k))
                {
                    int keyCode = TranslateKeyCode(k);
                    Console.Out.WriteLine("PRESSED KEY: " + k + " / " + keyCode);
                    if (keyCode >= 0)
                    {
                        keyEvents.Add(keyCode | (int) KeyCode.KEYFLAG_PRESSED);
                    }
                }
            }
        }

        public int[] getKeyboardEvents()
        {
            if (keyEvents.Count < 1)
            {
                return null;
            }

            int[] events = keyEvents.ToArray();
            keyEvents.Clear();

            return events;
        }

        public int TranslateKeyCode(Keys k)
        {
            return DesktopKeymap.Translate((int) k);
        }

        private void updateTextureBuffer()
        {
            if (pixelBufferChanged)
            {
                pixelBufferChanged = false;
                textureBuffer.SetData<Color>(0, new Rectangle(0, 0, emu.Video.width, emu.Video.height), colorBuffer, 0, colorBuffer.Length);

                statistics.update();
            }
        }

        public float EmulationSpeed
        {
            get { return emu.EmulationSpeed; }
        }

        public float ScreenWidth
        {
            get { return GraphicsDevice.Viewport.Width; }
        }

        public float ScreenHeight
        {
            get { return GraphicsDevice.Viewport.Height; }
        }

    }
}
