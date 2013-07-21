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
	public class EmulatorUI : EmulatorIOAdapter
	{
		private EmulatorApplication emu;
		
        private GraphicsContext graphics;
        private ShaderProgram program;
        private Texture2D texture;
		
		private Font normalFont;

        private const float VIDEO_ZOOM = 2.7f;
        private const bool VIDEO_FILTER = true;
        private const int VIDEO_WIDTH  = 0x180;  // 384px
        private const int VIDEO_HEIGHT = 0x110;  // 272px
        private byte[] videoBuffer;

        private volatile bool running;
        private volatile bool textureNeedsUpdate;

        private const int STATE_JOYSTICK_NONE     = 0x0;

        private const int STATE_JOYSTICK_LEFT     = 0x1;
        private const int STATE_JOYSTICK_RIGHT    = 0x2;
        private const int STATE_JOYSTICK_UP       = 0x4;
        private const int STATE_JOYSTICK_DOWN     = 0x8;

        private const int STATE_JOYSTICK_BUTTON_1 = 0x10;
        private const int STATE_JOYSTICK_BUTTON_2 = 0x20;
        private const int STATE_JOYSTICK_BUTTON_3 = 0x40;
        private const int STATE_JOYSTICK_BUTTON_4 = 0x80;
        private const int STATE_JOYSTICK_BUTTON_5 = 0x100;
        private const int STATE_JOYSTICK_BUTTON_6 = 0x200;
        private const int STATE_JOYSTICK_BUTTON_7 = 0x400;
        private const int STATE_JOYSTICK_BUTTON_8 = 0x800;

        private volatile uint stateBuffer = STATE_JOYSTICK_NONE;
        private volatile uint remoteState = 0xffffffff;

        VertexBuffer vertexBuffer;

        float[] vertices = new float[12];
        float[] screenTexcoords = new float[8];
		float[] defaultTexcoords = new float[8];
        float[] colors = new float[16];

        byte[] palette_red = {
         0x00, 0xff, 0xff, 0x00, 0xff, 0x00, 0x00, 0xff, 0xff, 0x80, 0xff, 0x40, 0x80, 0x80, 0x80, 0xc0
        };

        byte[] palette_green = {
         0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x80, 0x40, 0x80, 0x40, 0x80, 0xff, 0x80, 0xc0
        };

        byte[] palette_blue = {
         0x00, 0xff, 0x00, 0xff, 0xff, 0x00, 0xff, 0x00, 0x00, 0x00, 0x80, 0x40, 0x80, 0x80, 0xff, 0xc0
        };
		
		protected class TextAlignment
		{
			public const int LEFT		= 0x1;
			public const int CENTER		= 0x2;
			public const int RIGHT		= 0x4;
			public const int TOP		= 0x10;
			public const int MIDDLE		= 0x20;
			public const int BOTTOM		= 0x40;
		};
		
		protected Statistics statistics;

        int vertexCount;

        public EmulatorUI()
        {
            running = false;
            textureNeedsUpdate = false;
        }

        protected void startup()
        {
            // Set up the graphics system
            graphics = new GraphicsContext();
            graphics.SetViewport(0, 0, graphics.Screen.Width, graphics.Screen.Height);
            graphics.SetClearColor(0.0f, 0.0f, 0.0f, 0.0f);
			graphics.Enable(EnableMode.Blend);
            graphics.SetBlendFunc(BlendFuncMode.Add, BlendFuncFactor.SrcAlpha, BlendFuncFactor.OneMinusSrcAlpha);

            program = new ShaderProgram("/Application/shaders/Simple.cgx");

            program.SetUniformBinding(0, "WorldViewProj");
            program.SetAttributeBinding(0, "a_Position");
            program.SetAttributeBinding(1, "a_TexCoord");
            program.SetAttributeBinding(2, "a_Color");

            Matrix4 unitScreenMatrix = new Matrix4(
                 2.0f / graphics.Screen.Width, 0.0f, 0.0f, 0.0f,
                 0.0f, -2.0f / graphics.Screen.Height,  0.0f, 0.0f,
                 0.0f, 0.0f, 1.0f, 0.0f,
                 -1.0f, 1.0f, 0.0f, 1.0f
            );

            program.SetUniformValue(0, ref unitScreenMatrix);

            videoBuffer = new byte[VIDEO_WIDTH * VIDEO_HEIGHT * 4];

            texture = new Texture2D(512, 512, false, PixelFormat.Rgba);
            texture.SetFilter(VIDEO_FILTER ? TextureFilterMode.Linear : TextureFilterMode.Nearest);

            float tx = (float) VIDEO_WIDTH / (float) texture.Width;
            float ty = (float) VIDEO_HEIGHT / (float) texture.Height;
			
			screenTexcoords = new float[] {
                0.0f, 0.0f, // 0 top left.
                0.0f, ty, // 1 bottom left.
                tx, 0.0f, // 2 top right.
                tx, ty, // 3 bottom right.
            };
			
            defaultTexcoords = new float[] {
                0.0f, 0.0f, // 0 top left.
                0.0f, 1.0f, // 1 bottom left.
                1.0f, 0.0f, // 2 top right.
                1.0f, 1.0f, // 3 bottom right.
            };
    
            colors = new float[] {
                1.0f,   1.0f,   1.0f,   1.0f,   // 0 top left.
                1.0f,   1.0f,   1.0f,   1.0f,   // 1 bottom left.
                1.0f,   1.0f,   1.0f,   1.0f,   // 2 top right.
                1.0f,   1.0f,   1.0f,   1.0f,   // 3 bottom right.
            };


            vertexCount = vertices.Length / 3;

            vertexBuffer = new VertexBuffer(vertexCount,
                                            VertexFormat.Float3,
                                            VertexFormat.Float2,
                                            VertexFormat.Float4);

            //vertexBuffer.SetVertices(0, vertices);
            //vertexBuffer.SetVertices(1, texcoords);
            //vertexBuffer.SetVertices(2, colors);
			
			/////
			
			int fontSize = 14;
			
			normalFont = new Font(FontAlias.System, fontSize, FontStyle.Regular);
			
			/////
			
			emu = new EmulatorApplication(this);
			emu.initialize();
			
			statistics = new Statistics();
        }
		
		private static Texture2D createTexture(string text, Font font, uint argb)
	    {
	        int width = font.GetTextWidth(text, 0, text.Length);
	        int height = font.Metrics.Height;
	
	        var image = new Image(ImageMode.Rgba,
	                              new ImageSize(width, height),
	                              new ImageColor(0, 0, 0, 0));
	
	        image.DrawText(text,
	                       new ImageColor((int)((argb >> 16) & 0xff),
	                                      (int)((argb >> 8) & 0xff),
	                                      (int)((argb >> 0) & 0xff),
	                                      (int)((argb >> 24) & 0xff)),
	                       font, new ImagePosition(0, 0));
	
	        var texture = new Texture2D(width, height, false, PixelFormat.Rgba);
	        texture.SetPixels(0, image.ToBuffer());
	        image.Dispose();
	
	        return texture;
	    }
		
		protected void drawText(string text, float x, float y, uint argb, int alignment=0)
		{
			Texture2D texture = createTexture (text, normalFont, argb);
			
			float x0 = x;
			float y0 = y;
			
			if (0 != (alignment & TextAlignment.CENTER)) x0 -= (float) (texture.Width / 2);
			if (0 != (alignment & TextAlignment.RIGHT))  x0 -= (float) texture.Width;
			
			if (0 != (alignment & TextAlignment.MIDDLE)) y0 -= (float) (texture.Height / 2);
			if (0 != (alignment & TextAlignment.BOTTOM)) y0 -= (float) texture.Height;
			
			float x1 = x0 + texture.Width;
			float y1 = y0 + texture.Height;
			
			drawTexture(texture, x0, y0, x1, y1, defaultTexcoords);
			
			texture.Dispose();
		}

        protected void shutdown()
        {
            running = false;

            vertexBuffer.Dispose();
            program.Dispose();
            texture.Dispose();
            graphics.Dispose();
        }

        protected virtual void Run()
        {
            running = true;

            while (true)
            {
                SystemEvents.CheckEvents();

                inputs();
                update();
                render();
            }
        }

        protected void inputs()
        {
            GamePadData gamePadData = GamePad.GetData(0);

            uint joystickState = STATE_JOYSTICK_NONE;

            if (0 != (gamePadData.Buttons & GamePadButtons.Cross)) joystickState |= STATE_JOYSTICK_BUTTON_1;
            if (0 != (gamePadData.Buttons & GamePadButtons.Square)) joystickState |= STATE_JOYSTICK_BUTTON_2;
            if (0 != (gamePadData.Buttons & GamePadButtons.Triangle)) joystickState |= STATE_JOYSTICK_BUTTON_3;
            if (0 != (gamePadData.Buttons & GamePadButtons.Circle)) joystickState |= STATE_JOYSTICK_BUTTON_4;

            if (0 != (gamePadData.Buttons & GamePadButtons.Select)) joystickState |= STATE_JOYSTICK_BUTTON_5;
            if (0 != (gamePadData.Buttons & GamePadButtons.Start)) joystickState |= STATE_JOYSTICK_BUTTON_6;
            if (0 != (gamePadData.Buttons & GamePadButtons.L)) joystickState |= STATE_JOYSTICK_BUTTON_7;
            if (0 != (gamePadData.Buttons & GamePadButtons.R)) joystickState |= STATE_JOYSTICK_BUTTON_8;

            if (0 != (gamePadData.Buttons & GamePadButtons.Left))  joystickState |= STATE_JOYSTICK_LEFT;
            if (0 != (gamePadData.Buttons & GamePadButtons.Right)) joystickState |= STATE_JOYSTICK_RIGHT;
            if (0 != (gamePadData.Buttons & GamePadButtons.Up))    joystickState |= STATE_JOYSTICK_UP;
            if (0 != (gamePadData.Buttons & GamePadButtons.Down))  joystickState |= STATE_JOYSTICK_DOWN;

            float stickX = gamePadData.AnalogLeftX;
            if (stickX < -0.1f) joystickState |= STATE_JOYSTICK_LEFT;
            if (stickX >  0.1f) joystickState |= STATE_JOYSTICK_RIGHT;

            float stickY = gamePadData.AnalogLeftY;
            if (stickY < -0.1f) joystickState |= STATE_JOYSTICK_UP;
            if (stickY >  0.1f) joystickState |= STATE_JOYSTICK_DOWN;

            //LOG_DEBUG("JOYSTICK STATE: " + joystickState);

            stateBuffer = joystickState;

            if (remoteState != stateBuffer)
            {
                //networkAdapter.sendData(BitConverter.GetBytes(stateBuffer));
            }

            //LOG_DEBUG("REMOTE STATE: " + remoteState + " / INTERNAL STATE: " + stateBuffer);

        }

        protected void update()
        {

        }

        protected void render()
        {
            if (textureNeedsUpdate)
            {
                texture.SetPixels(0, videoBuffer, 0, 0, VIDEO_WIDTH, VIDEO_HEIGHT);
                textureNeedsUpdate = false;
				
				statistics.update();
            }

            graphics.Clear();
			
			Quad screenRect = layoutScreen();
			drawScreen (screenRect);
			drawOverlay();

            graphics.SwapBuffers();
        }
		
		protected virtual Quad layoutScreen()
		{
            float scale = VIDEO_ZOOM;
            float scaledWidth = (float) VIDEO_WIDTH * scale;
            float scaledHeight = (float) VIDEO_HEIGHT * scale;
            float xCenter = (float) graphics.Screen.Width / 2.0f;
            float yCenter = (float) graphics.Screen.Height / 2.0f;

            float x0 = xCenter - scaledWidth / 2.0f;
            float x1 = x0 + scaledWidth;

            float y0 = yCenter - scaledHeight / 2.0f;
            float y1 = y0 + scaledHeight;
			
			return new Quad(x0, y0, x1, y1);
		}
		
		protected virtual void drawScreen(Quad screenRect)
		{
			drawTexture(texture, screenRect.x0, screenRect.y0, screenRect.x1, screenRect.y1, screenTexcoords);
		}
		
		protected virtual void drawOverlay()
		{
		}

		protected void drawTexture(Texture2D texture, float x0, float y0, float x1, float y1)
		{
			drawTexture(texture, x0, y0, x1, y1, defaultTexcoords);
		}
		
		protected void drawTexture(Texture2D texture, float x0, float y0, float x1, float y1, float[] texcoords)
		{
		
            vertices[0]=x0;   // x0
            vertices[1]=y0;   // y0
            vertices[2]=0.0f;   // z0

            vertices[3]=x0;   // x1
            vertices[4]=y1;      // y1
            vertices[5]=0.0f;   // z1

            vertices[6]=x1;      // x2
            vertices[7]=y0;   // y2
            vertices[8]=0.0f;   // z2

            vertices[9]=x1;      // x3
            vertices[10]=y1;     // y3
            vertices[11]=0.0f;  // z3

            vertexBuffer.SetVertices(0, vertices);
            vertexBuffer.SetVertices(1, texcoords);
            vertexBuffer.SetVertices(2, colors);
			
            graphics.SetShaderProgram(program);
            graphics.SetVertexBuffer(0, vertexBuffer);
            graphics.SetTexture(0, texture);
            graphics.DrawArrays(DrawMode.TriangleStrip, 0, vertexCount);
			
		}
		
		public void decode4Bit(byte[] data, int offset)
		{
            int requiredBytes = 4 + VIDEO_WIDTH * VIDEO_HEIGHT / 2; // 4 bytes header + 4 bits per pixel screen

            if (data.Length < requiredBytes) return; // not enough data

            remoteState = BitConverter.ToUInt32(data, 0);

            int i;
            int src = offset;
            for (int y=0; y<VIDEO_HEIGHT; y++)
            {
                for (int x=0; x<VIDEO_WIDTH; x+=2)
                {
                    i = (y * VIDEO_WIDTH + x) * 4;

                    byte b;

                    b = data[src++];

                    {
                        int c = (b & 0x0f);

                        videoBuffer[i+0] = palette_red[c];
                        videoBuffer[i+1] = palette_green[c];
                        videoBuffer[i+2] = palette_blue[c];
                        videoBuffer[i+3] = 255;
                    }

                    {
                        int c = ((b & 0xf0) >> 4);

                        videoBuffer[i+4] = palette_red[c];
                        videoBuffer[i+5] = palette_green[c];
                        videoBuffer[i+6] = palette_blue[c];
                        videoBuffer[i+7] = 255;
                    }

                }
            }

            textureNeedsUpdate = true;
		}
		
		public void decode8Bit(byte[] data, int offset)
		{
            int requiredBytes = VIDEO_WIDTH * VIDEO_HEIGHT; // 8 bits per pixel

            if (data.Length < requiredBytes) return; // not enough data
			
			int i = 0;
            int src = offset;
            for (int y=0; y<VIDEO_HEIGHT; y++)
            {
                for (int x=0; x<VIDEO_WIDTH; x++)
                {
                    int c = data[src++];

                    videoBuffer[i+0] = palette_red[c];
                    videoBuffer[i+1] = palette_green[c];
                    videoBuffer[i+2] = palette_blue[c];
                    videoBuffer[i+3] = 255;
					
					i += 4;
                }
            }
			
			textureNeedsUpdate = true;
		}

        public void onNewFrame(byte[] data)
        {
            if (false == running) return;

            // System.Console.Out.WriteLine ("New Frame: " + data.Length + " bytes");

            if (textureNeedsUpdate) return; // already updated
			
			//decode4Bit(data, 4);
			
			decode8Bit(data, 0);

        }

        public static void LOG_DEBUG(String text)
        {
            System.Console.WriteLine(text);
        }
		
        public int[] getKeyboardEvents()
		{
			int[] events = new int[] { 0x0 };
			
			return null;
		}
		
		public float EmulationSpeed
		{
			get { return emu.EmulationSpeed; }
		}
		
		public float ScreenWidth
		{
			get { return graphics.Screen.Width; }
		}
		
		public float ScreenHeight
		{
			get { return graphics.Screen.Height; }
		}
	}
}

