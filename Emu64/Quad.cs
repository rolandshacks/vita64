using System;

namespace C64Emu
{
	public class Quad
	{
		public float x0;
		public float x1;
		public float y0;
		public float y1;
		
		public Quad()
		{
			x0 = x1 = y0 = y1 = 0.0f;
		}
		
		public Quad(float x0, float y0, float x1, float y1)
		{
			this.x0 = x0;
			this.y0 = y0;
			this.x1 = x1;
			this.y1 = y1;
		}
		
		public float Width
		{
			get { return x1-x0; }
		}

		public float Height
		{
			get { return y1-y0; }
		}
	}
}

