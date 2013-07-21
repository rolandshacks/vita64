using System;
using System.Diagnostics;
using System.IO;
//using SdlDotNet;

namespace C64Lib.Core
{
    public class Frodo
    {
        // TODO
        //#region Private Constants
        //string BASIC_ROM_FILE	= "Basic ROM";
        //string KERNAL_ROM_FILE	= "Kernal ROM";
        //string CHAR_ROM_FILE	= "Char ROM";
        //string FLOPPY_ROM_FILE  = "1541 ROM";
        //#endregion

        //#region Public methods

        //public Frodo()
        //{
        //    _TheC64 = new C64();
        //    _TheC64.Initialize();
        //}

        //public void ReadyToRun()
        //{
        //    if (load_rom_files())
        //    {
        //        _TheC64.Run();
        //    }
        //}

        //#endregion

        //#region Public Properties

        //public C64 TheC64
        //{
        //    get { return _TheC64; }
        //    set { _TheC64 = value; }
        //}

        //#endregion

        // TODO
        //private static void load_rom_file(string romFilename, byte[] memoryBuffer, int byteCount)
        //{
        //    // Load the ROM data from a binary file on disk
        //    using (Stream file = new FileStream(romFilename, FileMode.Open))
        //    using (BinaryReader br = new BinaryReader(file))
        //    {
        //        br.Read(memoryBuffer, 0, byteCount);
        //    }
        //}

        // TODO
        //private bool load_rom_files()
        //{
        //    // Load Basic ROM
        //    try
        //    {
        //        load_rom_file(BASIC_ROM_FILE, TheC64.Basic, 0x2000);
        //    }
        //    catch (IOException)
        //    {
        //        TheC64.TheDisplay.ShowRequester("Can't read 'Basic ROM'.", "Quit");
        //        return false;
        //    }

        //    // Load Kernal ROM
        //    try
        //    {
        //        load_rom_file(KERNAL_ROM_FILE, TheC64.Kernal, 0x2000);
        //    }
        //    catch (IOException)
        //    {
        //        TheC64.TheDisplay.ShowRequester("Can't read 'Kernal ROM'.", "Quit");
        //        return false;
        //    }
                      

        //    // Load Char ROM
        //    try
        //    {
        //        load_rom_file(CHAR_ROM_FILE, TheC64.Char, 0x1000);
        //    }
        //    catch (IOException)
        //    {
        //        TheC64.TheDisplay.ShowRequester("Can't read 'Char ROM'.", "Quit");
        //        return false;
        //    }          

        //    // Load 1541 ROM
        //    try
        //    {
        //        load_rom_file(FLOPPY_ROM_FILE, TheC64.ROM1541, 0x4000);
        //    }
        //    catch (IOException)
        //    {
        //        TheC64.TheDisplay.ShowRequester("Can't read '1541 ROM'.", "Quit");
        //        return false;
        //    }          

        //    return true;
        //}

        //C64 _TheC64;

        // TODO
        //public void Shutdown()
        //{
        //    Debug.WriteLine("Frodo: Shutdown");

        //    Video.Close();
        //    Events.QuitApplication();
        //}
    }
}
