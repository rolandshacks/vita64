using C64Lib.Utils;

namespace C64Lib.Core
{
    public abstract class Drive
    {
        public Drive(IEC iec)
        {
            the_iec = iec;
            LED = DriveLEDState.LedOff;
            Ready = false;
            set_error(ErrorCode1541.ERR_STARTUP);
        }

        public abstract byte Open(int channel, byte[] filename);

        public abstract byte Close(int channel);

        public abstract byte Read(int channel, ref byte abyte);

        public abstract byte Write(int channel, byte abyte, bool eoi);

        public abstract void Reset();

        #region public properties

        public DriveLEDState LED
        {
            get { return _LED; }
            set { _LED = value; }
        }

        public bool Ready
        {
            get { return _ready; }
            set { _ready = value; }
        }

        #endregion

        private DriveLEDState _LED;			// Drive LED state
        private bool _ready;			// Drive is ready for operation

        protected void set_error(ErrorCode1541 error)
        {

            _errors1541.CurrentItemIndex = (int)error;

            #region Old Code
            //if (error_ptr_buf != null)
            //{
            //    error_ptr_buf.Dispose();
            //}

            //unsafe
            //{
            //    error_ptr_buf = Errors_1541[(int)error];
            //    error_ptr = error_ptr_buf;
            //    error_len = Errors_1541[(int)error].Length;
            //}

            #endregion


            // Set drive condition
            if (error != ErrorCode1541.ERR_OK)
                if (error == ErrorCode1541.ERR_STARTUP)
                    LED = DriveLEDState.LedOff;
                else
                    LED = DriveLEDState.LedError;
            else if (LED == DriveLEDState.LedError)
                LED = DriveLEDState.LedOff;

            the_iec.UpdateLEDs();
        }

        //private BytePtr error_ptr_buf;

        //unsafe protected byte* error_ptr;	    // Pointer within error message	
        //protected int error_len;		        // Remaining length of error message


        private StringTable _errors1541 = new StringTable() 
        { 
	        "00, OK,00,00\r",
	        "25,WRITE ERROR,00,00\r",
	        "26,WRITE PROTECT ON,00,00\r",
	        "30,SYNTAX ERROR,00,00\r",
	        "33,SYNTAX ERROR,00,00\r",
	        "60,WRITE FILE OPEN,00,00\r",
	        "61,FILE NOT OPEN,00,00\r",
	        "62,FILE NOT FOUND,00,00\r",
	        "67,ILLEGAL TRACK OR SECTOR,00,00\r",
	        "70,NO CHANNEL,00,00\r",
	        "73,CBM DOS V2.6 1541,00,00\r",
	        "74,DRIVE NOT READY,00,00\r"
        };

        //// 1541 error messages
        //string[] Errors_1541 = {
        //    "00, OK,00,00\r",
        //    "25,WRITE ERROR,00,00\r",
        //    "26,WRITE PROTECT ON,00,00\r",
        //    "30,SYNTAX ERROR,00,00\r",
        //    "33,SYNTAX ERROR,00,00\r",
        //    "60,WRITE FILE OPEN,00,00\r",
        //    "61,FILE NOT OPEN,00,00\r",
        //    "62,FILE NOT FOUND,00,00\r",
        //    "67,ILLEGAL TRACK OR SECTOR,00,00\r",
        //    "70,NO CHANNEL,00,00\r",
        //    "73,CBM DOS V2.6 1541,00,00\r",
        //    "74,DRIVE NOT READY,00,00\r"
        //};

        private IEC the_iec;		// Pointer to IEC object
    }
}
