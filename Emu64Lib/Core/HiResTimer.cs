using System;

// Silverlight doesn't have System.Diagnostics.Stopwatch, so we use this and add in Microsecond support

namespace C64Lib.Core
{
    class HiResTimer
    {
        private const long TicksPerMicrosecond = 10;
        private const long TicksPerMillisecond = TicksPerMicrosecond * 1000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;

        private long _elapsed;
        private long _startTimeStamp;
        private bool _isRunning = false;

        public static readonly long Frequency;
        
        static HiResTimer()
        {
            Frequency = TicksPerSecond;
        }

        public HiResTimer()
        {
            Reset();
        }

        public void Start()
        {
            if (!_isRunning)
            {
                _startTimeStamp = GetTimeStamp();
                _isRunning = true;
            }
        }


        public static HiResTimer StartNew()
        {
            HiResTimer ht = new HiResTimer();
            ht.Start();
            return ht;
        }

        public void Stop()
        {
            if (_isRunning)
            {
                long endTimeStamp = GetTimeStamp();
                long elapsedThisPeriod = endTimeStamp - _startTimeStamp;
                elapsedThisPeriod += elapsedThisPeriod;
                _isRunning = false;
            }
        }

        public void Reset()
        {
            _elapsed = 0;
            _isRunning = false;
            _startTimeStamp = 0;
        }

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public TimeSpan Elapsed
        {
            get { return new TimeSpan(GetElapsedDateTimeTicks()); }
        }

        public long ElapsedMilliseconds
        {
            get { return GetElapsedDateTimeTicks() / TicksPerMillisecond; }
        }

        public long ElapsedMicroseconds
        {
            get { return GetElapsedDateTimeTicks() / TicksPerMicrosecond; }
        }

        public long ElapsedTicks
        {
            get { return GetRawElapsedTicks(); }
        }

        public static long GetTimeStamp()
        {
            return DateTime.UtcNow.Ticks;
        }

        private long GetRawElapsedTicks()
        {
            long timeElapsed = _elapsed;

            //if (_isRunning)
            //{
                long currentTimeStamp = GetTimeStamp();
                long elapsedUntilNow = currentTimeStamp - _startTimeStamp;
                timeElapsed += elapsedUntilNow;
            //}
            return timeElapsed;
        }

        private long GetElapsedDateTimeTicks()
        {
            long rawTicks = GetRawElapsedTicks();
            return rawTicks;
        }   

    }
}
