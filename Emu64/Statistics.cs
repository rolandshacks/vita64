using System;

namespace C64Emu
{
	public class Statistics
	{
		int		startTime;
		int		cycleTime;
		int 	frameCounter;
		double	framesPerSecond;
		
		public Statistics()
		{
			cycleTime = 2000;
			
			init();
		}
		
		private int getTime()
		{
			return Environment.TickCount;
		}
		
		public void init()
		{
			frameCounter = 0;
			framesPerSecond = 0.0;
			
			startTime = getTime();
		}
		
		public void update()
		{
			update(1);
			
		}
		
		public void update(int renderedFrames)
		{
			frameCounter += renderedFrames;
			
			int currentTime = getTime();
			int elapsedTime = currentTime - startTime;
			
			if (elapsedTime >= cycleTime)
			{
				framesPerSecond = (double) frameCounter * 1000.0 / (double) cycleTime;
				frameCounter = 0;
				startTime = currentTime;
			}
		}
		
		public int FramesPerSecond 
		{
			get { return (int) framesPerSecond; }
		}
	}
}

