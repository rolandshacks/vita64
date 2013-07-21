using System;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using C64Lib.Memory;
using C64Lib.Utils;

namespace C64Lib.Core
{
    // ref http://mediasrv.ns.ac.yu/extra/fileformat/emulator/d64/d64.txt
    public class D64Drive : Drive, IDisposable
    {
        #region constants

        // Number of tracks/sectors
        public const int NUM_TRACKS = 35;
        public const int NUM_SECTORS = 683;

        // Number of sectors of each track
        public static readonly int[] num_sectors = {
	        0,
	        21,21,21,21,21,21,21,21,21,21,21,21,21,21,21,21,21,
	        19,19,19,19,19,19,19,
	        18,18,18,18,18,18,
	        17,17,17,17,17
        };

        // Sector offset of start of track in .d64 file
        public static readonly int[] sector_offset = {
	        0,
	        0,21,42,63,84,105,126,147,168,189,210,231,252,273,294,315,336,
	        357,376,395,414,433,452,471,
	        490,508,526,544,562,580,
	        598,615,632,649,666
        };

        #endregion



        #region private fields

        string _originalD64FileName;
        //string orig_d64_name;                       // Original path of .d64 file

        Stream the_file;			                // File pointer for .d64 file

        // TODO
        //BytePtr ram;				                // 2KB 1541 RAM
        Drive1541Ram _ram = new Drive1541Ram();

        // TODO
        //unsafe BAM* bam;				            // Pointer to BAM
        Bam bam;
        //int _bamIndex;  // index into Drive1541Ram for BAM
        RamBytePointer _bamPointer;

        //Directory dir;			                    // Buffer for directory blocks

        ChannelMode[] chan_mode = new ChannelMode[16];		// Channel mode
        int[] chan_buf_num = new int[16];	        // Buffer number of channel (for direct access channels)
        
        // TODO

        byte[][] chan_buf = new byte[16][];	        // Pointer to buffer
        //BytePtr[] chan_buf_alloc = new BytePtr[16];

        // TODO
        //unsafe byte*[] buf_ptr = new byte*[16];		// Pointer in buffer
        int[] bufIndex = new int[16];

        int[] buf_len = new int[16];		        // Remaining bytes in buffer

        bool[] buf_free = new bool[4];		        // Buffer 0..3 free?

        // TODO
        //BytePtr cmd_buffer = new BytePtr(44);	    // Buffer for incoming command strings
        int cmd_len;			                    // Length of received command

        int image_header;		                    // Length of .d64 file header

        byte[] error_info = new byte[683];	        // Sector error information (1 byte/sector)

        #endregion


        #region public methods

        // TODO
        public D64Drive(IEC iec, string fileName, Stream fileStream)
            : base(iec)
        {
            the_file = null;

            Ready = false;
            _originalD64FileName = fileName;

            for (int i = 0; i < chan_mode.Length - 1; i++)
            {
                chan_mode[i] = ChannelMode.CHMOD_FREE;
            }
            chan_mode[15] = ChannelMode.CHMOD_COMMAND;

            // Open .d64 file
            open_close_d64_file(fileName, fileStream);

            if (the_file != null)
            {
                // Allocate 1541 RAM
                //ram = new BytePtr(0x800);
                //unsafe
                //{
                
                _bamPointer = _ram.NewBytePointer(_ram.BamStartIndex);
                
                //bam = new Bam(_ram.Read(_ram.BamStartIndex, 256));

                // TODO: Will need to copy BAM to 1541 memory or do something else to make
                // sure that is mapped in there.

                    //bam = (BAM*)(ram.Pointer + 0x700);
                //}

                Reset();

                Ready = true;
            }
        }

        public override byte Open(int channel, byte[] filename)
        {
            //using (BytePtr filename = new BytePtr(afilename))
            //{
                set_error(ErrorCode1541.ERR_OK);

                // Channel 15: execute file name as command
                if (channel == 15)
                {
                    execute_command(filename);
                    return (byte)C64StatusCode.ST_OK;
                }

                if (chan_mode[channel] != ChannelMode.CHMOD_FREE)
                {
                    set_error(ErrorCode1541.ERR_NOCHANNEL);
                    return (byte)C64StatusCode.ST_OK;
                }

                if (filename[0] == '$')
                    if (channel != 0)
                        return open_file_ts(channel, 18, 0);
                    else
                    {
                        //unsafe
                        //{
                            return OpenDirectory(filename.CopySubset(1));
                        //}
                    }

                if (filename[0] == '#')
                    return open_direct(channel, filename);

                return OpenFile(channel, filename);
            //}
        }

        public override byte Close(int channel)
        {
            if (channel == 15)
            {
                close_all_channels();
                return (byte)C64StatusCode.ST_OK;
            }

            switch (chan_mode[channel])
            {
                case ChannelMode.CHMOD_FREE:
                    break;

                case ChannelMode.CHMOD_DIRECT:
                    free_buffer(chan_buf_num[channel]);
                    chan_buf[channel] = null;
                    chan_mode[channel] = ChannelMode.CHMOD_FREE;
                    break;

                default:
                    DeallocateChannelBuffer(channel);
                    chan_mode[channel] = ChannelMode.CHMOD_FREE;
                    break;
            }
            return (byte)C64StatusCode.ST_OK;
        }

        // TODO
        public override byte Read(int channel, ref byte abyte)
        {
            switch (chan_mode[channel])
            {
                case ChannelMode.CHMOD_COMMAND:
                    // TODO
                    //abyte = *error_ptr++;
                    //if (--error_len != 0)
                    //    return (byte)C64StatusCode.ST_OK;
                    //else
                    //{
                    //    set_error(ErrorCode1541.ERR_OK);
                    //    return (byte)C64StatusCode.ST_EOF;
                    //}

                case ChannelMode.CHMOD_FILE:
                    // Read next block if necessary
                    if (chan_buf[channel][0] != 0 && buf_len[channel] == 0)
                    {
                        if (!read_sector(chan_buf[channel][0], chan_buf[channel][1], chan_buf[channel]))
                            return (byte)C64StatusCode.ST_READ_TIMEOUT;

                        //buf_ptr[channel] = chan_buf[channel] + 2;
                        bufIndex[channel] = 2;


                        // Determine block length
                        buf_len[channel] = chan_buf[channel][0] != 0 ? 254 : (byte)chan_buf[channel][1] - 1;
                    }

                    if (buf_len[channel] > 0)
                    {
                        //abyte = *buf_ptr[channel]++;

                        abyte = chan_buf[channel][bufIndex[channel]];
                        bufIndex[channel] += 1;

                        //if (--buf_len[channel] == 0 && chan_buf[channel][0] == 0)
                        //    return (byte)C64StatusCode.ST_EOF;
                        //else
                        //    return (byte)C64StatusCode.ST_OK;

                        buf_len[channel] -= 1;
                        if (buf_len[channel] == 0 && chan_buf[channel][0] == 0)
                            return (byte)C64StatusCode.ST_EOF;
                        else
                            return (byte)C64StatusCode.ST_OK;
                    }
                    else
                        return (byte)C64StatusCode.ST_READ_TIMEOUT;

                case ChannelMode.CHMOD_DIRECTORY:
                case ChannelMode.CHMOD_DIRECT:
                    if (buf_len[channel] > 0)
                    {
                        //abyte = *buf_ptr[channel]++;
                        //if (--buf_len[channel] != 0)
                        //    return (byte)C64StatusCode.ST_OK;
                        //else
                        //    return (byte)C64StatusCode.ST_EOF;

                        abyte = chan_buf[channel][bufIndex[channel]];
                        bufIndex[channel] += 1;

                        buf_len[channel] -= 1;
                        if (buf_len[channel] != 0)
                            return (byte)C64StatusCode.ST_OK;
                        else
                            return (byte)C64StatusCode.ST_EOF;


                    }
                    else
                        return (byte)C64StatusCode.ST_READ_TIMEOUT;
            }

            return (byte)C64StatusCode.ST_READ_TIMEOUT;

            //// TEMP!
            //return (byte)1;
        }

        // TODO
        public override byte Write(int channel, byte abyte, bool eoi)
        {
            //switch (chan_mode[channel])
            //{
            //    case ChannelMode.CHMOD_FREE:
            //        set_error(ErrorCode1541.ERR_FILENOTOPEN);
            //        break;

            //    case ChannelMode.CHMOD_COMMAND:
            //        // Collect characters and execute command on EOI
            //        if (cmd_len >= 40)
            //            return (byte)C64StatusCode.ST_TIMEOUT;

            //        cmd_buffer[cmd_len++] = abyte;

            //        if (eoi)
            //        {
            //            cmd_buffer[cmd_len++] = 0;
            //            cmd_len = 0;
            //            execute_command(cmd_buffer);
            //        }
            //        return (byte)C64StatusCode.ST_OK;

            //    case ChannelMode.CHMOD_DIRECTORY:
            //        set_error(ErrorCode1541.ERR_WRITEFILEOPEN);
            //        break;
            //}
            return (byte)C64StatusCode.ST_TIMEOUT;
        }

        // TODO
        public override void Reset()
        {
            close_all_channels();

            byte[] buffer = Bam.AllocateBuffer();
            read_sector(18, 0, buffer);

            bam = new Bam(buffer);

            //cmd_len = 0;
            //for (int i = 0; i < buf_free.Length; i++)
            //    buf_free[i] = true;

            set_error(ErrorCode1541.ERR_STARTUP);
        }

        #endregion

        #region private methods

        // ok
        private void open_close_d64_file(string d64name, Stream fileStream)
        {
            long size;
            byte[] magic = new byte[4];

            // Close old .d64, if open
            if (the_file != null)
            {
                close_all_channels();
                the_file.Dispose();
                the_file = null;
            }

            if (null == d64name || d64name.Length < 1)
            {
                return;
            }

            if (null != fileStream)
            {
                the_file = fileStream;
            }
            else
            {
				try
				{
                	the_file = new FileStream(d64name, FileMode.Open, FileAccess.Read);
				} 
				catch (DirectoryNotFoundException)
				{
					the_file = null;
					return;
				}
            }

            // Open new .d64 file
            // Check length
            size = the_file.Length;

            // Check length
            if (size < NUM_SECTORS * 256)
            {
                the_file.Dispose();
                the_file = null;
                return;
            }

            // x64 image?
            the_file.Read(magic, 0, 4);
            if (magic[0] == 0x43 && magic[1] == 0x15 && magic[2] == 0x41 && magic[3] == 0x64)
                image_header = 64;
            else
                image_header = 0;

            // Preset error info (all sectors no error)
            Array.Clear(error_info, 0, error_info.Length);

            // Load sector error info from .d64 file, if present
            if (image_header == 0 && size == NUM_SECTORS * 257)
            {
                the_file.Seek(NUM_SECTORS * 256, SeekOrigin.Begin);
                the_file.Read(error_info, 0, NUM_SECTORS);
            }
        }

        byte OpenFile(int channel, byte[] filename)
        {
            FileAccessMode filemode = FileAccessMode.FMODE_READ;
            FileType filetype = FileType.FTYPE_PRG;
            int track = -1;
            int sector = -1;

            C64InternalFileSpec fileSpec = new C64InternalFileSpec(filename);

            // Channel 0 is READ PRG, channel 1 is WRITE PRG
            if (channel == 0)
            {
                filemode = FileAccessMode.FMODE_READ;
                filetype = FileType.FTYPE_PRG;
            }
            if (channel == 1)
            {
                filemode = FileAccessMode.FMODE_WRITE;
                filetype = FileType.FTYPE_PRG;
            }

            // Allow only read accesses
            if (filemode != FileAccessMode.FMODE_READ)
            {
                set_error(ErrorCode1541.ERR_WRITEPROTECT);
                return (byte)C64StatusCode.ST_OK;
            }

            // Find file in directory and open it
            if (FindFile(fileSpec.Name, ref track, ref sector))
                return open_file_ts(channel, track, sector);
            else
                set_error(ErrorCode1541.ERR_FILENOTFOUND);

            return (byte)C64StatusCode.ST_OK;

#if false
            //using (BytePtr plainname = new BytePtr(256))
            //{

            //    FileAccessMode filemode = FileAccessMode.FMODE_READ;
            //    FileType filetype = FileType.FTYPE_PRG;
            //    int track = -1, sector = -1;

            //    convert_filename(filename, plainname, ref filemode, ref filetype);

            //    // Channel 0 is READ PRG, channel 1 is WRITE PRG
            //    if (channel == 0)
            //    {
            //        filemode = FileAccessMode.FMODE_READ;
            //        filetype = FileType.FTYPE_PRG;
            //    }
            //    if (channel == 1)
            //    {
            //        filemode = FileAccessMode.FMODE_WRITE;
            //        filetype = FileType.FTYPE_PRG;
            //    }

            //    // Allow only read accesses
            //    if (filemode != FileAccessMode.FMODE_READ)
            //    {
            //        set_error(ErrorCode1541.ERR_WRITEPROTECT);
            //        return (byte)C64StatusCode.ST_OK;
            //    }

            //    // Find file in directory and open it
            //    if (find_file(plainname, ref track, ref sector))
            //        return open_file_ts(channel, track, sector);
            //    else
            //        set_error(ErrorCode1541.ERR_FILENOTFOUND);

                //return (byte)C64StatusCode.ST_OK;
            //}
#endif
        }

#if false
        //unsafe void convert_filename(BytePtr srcname, BytePtr destname, ref FileAccessMode filemode, ref FileType filetype)
        //{
        //    byte* p;

        //    // Search for ':', p points to first character after ':'
        //    if ((p = CharFunctions.strchr(srcname, ':')) != null)
        //        p++;
        //    else
        //        p = srcname;

        //    // Remaining string -> destname
        //    CharFunctions.strncpy(destname, srcname, p);

        //    // Look for mode parameters seperated by ','
        //    p = destname;
        //    while ((p = CharFunctions.strchr(p, ',')) != null)
        //    {

        //        // Cut string after the first ','
        //        *p++ = 0;
        //        switch ((Char)(*p))
        //        {
        //            case 'P':
        //                filetype = FileType.FTYPE_PRG;
        //                break;
        //            case 'S':
        //                filetype = FileType.FTYPE_SEQ;
        //                break;
        //            case 'U':
        //                filetype = FileType.FTYPE_USR;
        //                break;
        //            case 'L':
        //                filetype = FileType.FTYPE_REL;
        //                break;
        //            case 'R':
        //                filemode = FileAccessMode.FMODE_READ;
        //                break;
        //            case 'W':
        //                filemode = FileAccessMode.FMODE_WRITE;
        //                break;
        //            case 'A':
        //                filemode = FileAccessMode.FMODE_APPEND;
        //                break;
        //        }
        //    }
        //}
#endif

        private bool FindFile(byte[] filename, ref int track, ref int sector)
        {
            int i;

            DirEntry de;
            Directory dd;
            
            byte[] buffer = Directory.AllocateBuffer();

            byte nextTrack = bam.dir_track;
            byte nextSector = bam.dir_sector;

            // Scan all directory blocks
            while (nextTrack != 0)
            {
                if (!read_sector(nextTrack, nextSector, buffer))
                    return false;

                dd = new Directory(buffer);

                nextTrack = dd.next_track;
                nextSector = dd.next_sector;

                // Scan all 8 entries of a block
                for (i = 0; i < 8; i++)
                {
                    de = dd.Entries[i];     // get next directory entry

                    track = de.track;
                    sector = de.sector;

                    if (de.type != 0)
                    {
                        bool isMatch = false;

                        if (filename[0] != 0)
                            isMatch = match(filename, de.name);

                        if (isMatch)
                            return true;
                    }
                }

            }
            return false;
        }

        private byte open_file_ts(int channel, int track, int sector)
        {
            AllocateChannelBuffer(channel, 256);
            chan_mode[channel] = ChannelMode.CHMOD_FILE;

            // On the next call to Read, the first block will be read
            chan_buf[channel][0] = (byte)track;
            chan_buf[channel][1] = (byte)sector;
            buf_len[channel] = 0;

            return (byte)C64StatusCode.ST_OK;
        }


#if false
        // TODO
        //unsafe bool find_file(BytePtr filename, ref int track, ref int sector)
        //{
        //    int i, j;
        //    byte* p, q;
        //    DirEntry* de;

        //    fixed (Directory* dd = &dir)
        //    {
        //        // Scan all directory blocks
        //        dir.next_track = bam->dir_track;
        //        dir.next_sector = bam->dir_sector;

        //        while (dir.next_track != 0)
        //        {
        //            if (!read_sector(dir.next_track, dir.next_sector, &dd->next_track))
        //                return false;

        //            DirEntry* ade = (DirEntry*)dd->entry;
        //            // Scan all 8 entries of a block
        //            for (j = 0; j < 8; j++)
        //            {
        //                de = &ade[j];
        //                track = de->track;
        //                sector = de->sector;

        //                if (de->type != 0)
        //                {
        //                    p = (byte*)filename;
        //                    q = de->name;
        //                    for (i = 0; i < 16 && (*p != 0); i++, p++, q++)
        //                    {
        //                        if (*p == '*')	// Wildcard '*' matches all following characters
        //                            return true;
        //                        if (*p != *q)
        //                        {
        //                            if (*p != '?') goto next_entry;	// Wildcard '?' matches single character
        //                            if (*q == 0xa0) goto next_entry;
        //                        }
        //                    }

        //                    if (i == 16 || *q == 0xa0)
        //                        return true;
        //                }
        //            next_entry: ;
        //            }
        //        }
        //    }
        //    return false;
        //}

        // TODO
        byte open_file_ts(int channel, int track, int sector)
        {
            //AllocateChannelBuffer(channel, 256);
            //chan_mode[channel] = ChannelMode.CHMOD_FILE;

            //// On the next call to Read, the first block will be read
            //chan_buf[channel][0] = (byte)track;
            //chan_buf[channel][1] = (byte)sector;
            //buf_len[channel] = 0;

            return (byte)C64StatusCode.ST_OK;
        }
#endif
        /*
         *  Prepare directory as BASIC program (channel 0)
         */


        //static readonly byte[] type_char_1 = Encoding.ASCII.GetBytes("DSPUREERSELQGRL?");
        //static readonly byte[] type_char_2 = Encoding.ASCII.GetBytes("EERSELQGRL??????");
        //static readonly byte[] type_char_3 = Encoding.ASCII.GetBytes("LQGRL???????????");
        static readonly byte[] type_char_1 = Encoding.UTF8.GetBytes("DSPUREERSELQGRL?");
        static readonly byte[] type_char_2 = Encoding.UTF8.GetBytes("EERSELQGRL??????");
        static readonly byte[] type_char_3 = Encoding.UTF8.GetBytes("LQGRL???????????");

        // Return true if name 'n' matches pattern 'p'
        static bool match(byte[] p, byte[] n)
        {
            if (p[0] == 0x00)		// Null pattern matches everything
                return true;

            int i = 0;

            do
            {
                if (p[i] == '*')	// Wildcard '*' matches all following characters
                    return true;

                if ((p[i] != n[i]) && (p[i] != '?'))	// Wildcard '?' matches single character
                    return false;

                i++;

            } while (i < p.Length && i < n.Length && n[i] != 0x00 && p[i] != 0x00);

            if (i == n.Length) 
                return true;

            return n[i] == 0xa0;

        }


#if false
        byte open_directory(byte[] pattern)
        {
            int i, j, n, m;
            byte* p, q;
            DirEntry* de;
            byte c;
            byte* tmppat;

            // Special treatment for "$0"
            if (pattern[0] == '0' && pattern[1] == 0)
                pattern += 1;

            // Skip everything before the ':' in the pattern
            if ((tmppat = CharFunctions.strchr(pattern, ':')) != null)
                pattern = tmppat + 1;

            AllocateChannelBuffer(0, 8192);

            p = buf_ptr[0] = chan_buf[0];

            chan_mode[0] = ChannelMode.CHMOD_DIRECTORY;

            // Create directory title
            *p++ = 0x01;	// Load address $0401 (from PET days :-)
            *p++ = 0x04;
            *p++ = 0x01;	// Dummy line link
            *p++ = 0x01;
            *p++ = 0;		// Drive number (0) as line number
            *p++ = 0;
            *p++ = 0x12;	// RVS ON
            *p++ = (byte)'\"';

            q = bam->disk_name;
            for (i = 0; i < 23; i++)
            {
                if ((c = *q++) == 0xa0)
                    *p++ = (byte)' ';		// Replace 0xa0 by space
                else
                    *p++ = c;
            }
            *(p - 7) = (byte)'\"';
            *p++ = 0;

            // Scan all directory blocks
            dir.next_track = bam->dir_track;
            dir.next_sector = bam->dir_sector;

            fixed (Directory* dd = &dir)
            {

                while (dir.next_track != 0x00)
                {
                    if (!read_sector(dir.next_track, dir.next_sector, &dd->next_track))
                        return (byte)C64StatusCode.ST_OK;

                    DirEntry* ade = (DirEntry*)dd->entry;
                    // Scan all 8 entries of a block
                    for (j = 0; j < 8; j++)
                    {
                        de = &ade[j];

                        if (de->type != 0 && match(pattern, de->name))
                        {
                            *p++ = 0x01; // Dummy line link
                            *p++ = 0x01;

                            *p++ = de->num_blocks_l; // Line number
                            *p++ = de->num_blocks_h;

                            *p++ = (byte)' ';
                            n = (de->num_blocks_h << 8) + de->num_blocks_l;
                            if (n < 10) *p++ = (byte)' ';
                            if (n < 100) *p++ = (byte)' ';

                            *p++ = (byte)'\"';
                            q = de->name;
                            m = 0;
                            for (i = 0; i < 16; i++)
                            {
                                if ((c = *q++) == 0xa0)
                                {
                                    if (m != 0)
                                        *p++ = (byte)' ';		// Replace all 0xa0 by spaces
                                    else
                                        m = *p++ = (byte)'\"';	// But the first by a '"'
                                }
                                else
                                    *p++ = c;
                            }
                            if (m != 0)
                                *p++ = (byte)' ';
                            else
                                *p++ = (byte)'\"';			// No 0xa0, then append a space

                            if ((de->type & 0x80) != 0)
                                *p++ = (byte)' ';
                            else
                                *p++ = (byte)'*';

                            *p++ = type_char_1[de->type & 0x0f];
                            *p++ = type_char_2[de->type & 0x0f];
                            *p++ = type_char_3[de->type & 0x0f];

                            if ((de->type & 0x40) != 0)
                                *p++ = (byte)'<';
                            else
                                *p++ = (byte)' ';

                            *p++ = (byte)' ';
                            if (n >= 10) *p++ = (byte)' ';
                            if (n >= 100) *p++ = (byte)' ';
                            *p++ = 0;
                        }
                    }
                }

            }
            // Final line
            q = p;
            for (i = 0; i < 29; i++)
                *q++ = (byte)' ';

            n = 0;
            for (i = 0; i < 35; i++)
                n += bam->bitmap[i * 4];

            *p++ = 0x01;		// Dummy line link
            *p++ = 0x01;
            *p++ = (byte)(n & 0xff);	// Number of free blocks as line number
            *p++ = (byte)((n >> 8) & 0xff);

            *p++ = (byte)'B';
            *p++ = (byte)'L';
            *p++ = (byte)'O';
            *p++ = (byte)'C';
            *p++ = (byte)'K';
            *p++ = (byte)'S';
            *p++ = (byte)' ';
            *p++ = (byte)'F';
            *p++ = (byte)'R';
            *p++ = (byte)'E';
            *p++ = (byte)'E';
            *p++ = (byte)'.';

            p = q;
            *p++ = 0;
            *p++ = 0;
            *p++ = 0;

            buf_len[0] = (int)(p - chan_buf[0]);

            return (byte)C64StatusCode.ST_OK;
        }



#endif

        public byte ReplaceSpace(byte source)
        {
            if (source == (byte)0xa0)
                return (byte)' ';
            else
                return source;
        }


        byte OpenDirectory(byte[] pattern)
        {
            int i, j, n, m;
            //byte* p, q;

            int pIndex;

            Directory dir;
            DirEntry de;

            byte c;
            //byte* tmppat;

            // Special treatment for "$0"
            if (pattern[0] == '0' && pattern[1] == 0)
            {
                // TODO!
                //pattern += 1;
            }

            // Skip everything before the ':' in the pattern
            // TODO
            //if ((tmppat = CharFunctions.strchr(pattern, ':')) != null)
            //    pattern = tmppat + 1;


            int channelBuffer = 0;
            AllocateChannelBuffer(channelBuffer, 8192);

            //p = buf_ptr[0] = chan_buf[0];
            pIndex = 0;

            chan_mode[0] = ChannelMode.CHMOD_DIRECTORY;

            // Create directory title
            chan_buf[channelBuffer][pIndex++] = 0x01;	// Load address $0401 (from PET days :-)
            chan_buf[channelBuffer][pIndex++] = 0x04;
            chan_buf[channelBuffer][pIndex++] = 0x01;	// Dummy line link
            chan_buf[channelBuffer][pIndex++] = 0x01;
            chan_buf[channelBuffer][pIndex++] = 0;		// Drive number (0) as line number
            chan_buf[channelBuffer][pIndex++] = 0;
            chan_buf[channelBuffer][pIndex++] = 0x12;	// RVS ON (reverse text)
            chan_buf[channelBuffer][pIndex++] = (byte)'\"';

            for (i = 0; i < 16; i++)
            {
                chan_buf[channelBuffer][pIndex++] = ReplaceSpace(bam.disk_name[i]);
                //if ((c = bam.disk_name[i]) == 0xa0)
                //    chan_buf[channelBuffer][pIndex++] = (byte)' ';		// Replace 0xa0 by space
                //else
                //    chan_buf[channelBuffer][pIndex++] = c;
            }

            chan_buf[channelBuffer][pIndex++] = ReplaceSpace(bam.pad_name[0]);
            chan_buf[channelBuffer][pIndex++] = ReplaceSpace(bam.pad_name[1]);

            chan_buf[channelBuffer][pIndex++] = ReplaceSpace(bam.id[0]);
            chan_buf[channelBuffer][pIndex++] = ReplaceSpace(bam.id[1]);

            chan_buf[channelBuffer][pIndex++] = ReplaceSpace(bam.pad1);

            chan_buf[channelBuffer][pIndex++] = ReplaceSpace(bam.fmt_char[0]);
            chan_buf[channelBuffer][pIndex++] = ReplaceSpace(bam.fmt_char[1]);


            //*(p - 7) = (byte)'\"';
            chan_buf[channelBuffer][pIndex - 7] = (byte)'\"';

            //*p++ = 0;
            chan_buf[channelBuffer][pIndex++] = 0;

            // Scan all directory blocks
            byte[] buffer = Directory.AllocateBuffer();


            byte nextTrack = bam.dir_track;
            byte nextSector = bam.dir_sector;

            // Scan all directory blocks
            while (nextTrack != 0)
            {
                if (!read_sector(nextTrack, nextSector, buffer))
                    return (byte)C64StatusCode.ST_OK;
                dir = new Directory(buffer);

                nextTrack = dir.next_track;
                nextSector = dir.next_sector;


                // Scan all 8 entries of a block
                for (i = 0; i < 8; i++)
                {
                    de = dir.Entries[i];

                    //string debugString = string.Format("Entry {0} : ", i);

                    if (de.type != 0 && match(pattern, de.name))
                    {
                        chan_buf[channelBuffer][pIndex++] = 0x01; // Dummy line link
                        chan_buf[channelBuffer][pIndex++] = 0x01;

                        chan_buf[channelBuffer][pIndex++] = de.num_blocks_l; // Line number
                        chan_buf[channelBuffer][pIndex++] = de.num_blocks_h;

                        chan_buf[channelBuffer][pIndex++] = (byte)' ';

                        n = (de.num_blocks_h << 8) + de.num_blocks_l;

                        if (n < 10) chan_buf[channelBuffer][pIndex++] = (byte)' ';
                        if (n < 100) chan_buf[channelBuffer][pIndex++] = (byte)' ';

                        chan_buf[channelBuffer][pIndex++] = (byte)'\"';

                        //q = de->name;
                        m = 0;

                        for (j = 0; j < 16; j++)
                        {
                            if ((c = de.name[j]) == 0xa0)
                            {
                                if (m != 0)
                                    chan_buf[channelBuffer][pIndex++] = (byte)' ';		// Replace all 0xa0 by spaces
                                else
                                    m = chan_buf[channelBuffer][pIndex++] = (byte)'\"';	// But the first by a '"'
                            }
                            else
                                chan_buf[channelBuffer][pIndex++] = c;


                            //debugString += (char)chan_buf[channelBuffer][pIndex - 1];
                        }

                        if (m != 0)
                            chan_buf[channelBuffer][pIndex++] = (byte)' ';
                        else
                            chan_buf[channelBuffer][pIndex++] = (byte)'\"';			// No 0xa0, then append a space

                        if ((de.type & 0x80) != 0)
                            chan_buf[channelBuffer][pIndex++] = (byte)' ';
                        else
                            chan_buf[channelBuffer][pIndex++] = (byte)'*';

                        chan_buf[channelBuffer][pIndex++] = type_char_1[de.type & 0x0f];
                        chan_buf[channelBuffer][pIndex++] = type_char_2[de.type & 0x0f];
                        chan_buf[channelBuffer][pIndex++] = type_char_3[de.type & 0x0f];

                        if ((de.type & 0x40) != 0)
                            chan_buf[channelBuffer][pIndex++] = (byte)'<';
                        else
                            chan_buf[channelBuffer][pIndex++] = (byte)' ';

                        chan_buf[channelBuffer][pIndex++] = (byte)' ';
                        if (n >= 10) chan_buf[channelBuffer][pIndex++] = (byte)' ';
                        if (n >= 100) chan_buf[channelBuffer][pIndex++] = (byte)' ';
                        chan_buf[channelBuffer][pIndex++] = 0;
                    }


                    //System.Diagnostics.Debug.WriteLine(debugString);

                }

            } 

            // Final line
            //q = p;
            int q = pIndex;
            for (i = 0; i < 29; i++)
                chan_buf[channelBuffer][q++] = (byte)' ';


            n = 0;
            for (i = 0; i < 35; i++)
                n += bam.bitmap[i * 4];

            chan_buf[channelBuffer][pIndex++] = 0x01;		// Dummy line link
            chan_buf[channelBuffer][pIndex++] = 0x01;
            chan_buf[channelBuffer][pIndex++] = (byte)(n & 0xff);	// Number of free blocks as line number
            chan_buf[channelBuffer][pIndex++] = (byte)((n >> 8) & 0xff);

            chan_buf[channelBuffer][pIndex++] = (byte)'B';
            chan_buf[channelBuffer][pIndex++] = (byte)'L';
            chan_buf[channelBuffer][pIndex++] = (byte)'O';
            chan_buf[channelBuffer][pIndex++] = (byte)'C';
            chan_buf[channelBuffer][pIndex++] = (byte)'K';
            chan_buf[channelBuffer][pIndex++] = (byte)'S';
            chan_buf[channelBuffer][pIndex++] = (byte)' ';
            chan_buf[channelBuffer][pIndex++] = (byte)'F';
            chan_buf[channelBuffer][pIndex++] = (byte)'R';
            chan_buf[channelBuffer][pIndex++] = (byte)'E';
            chan_buf[channelBuffer][pIndex++] = (byte)'E';
            chan_buf[channelBuffer][pIndex++] = (byte)'.';

            //p = q;
            //*p++ = 0;
            //*p++ = 0;
            //*p++ = 0;
            pIndex = q;
            chan_buf[channelBuffer][pIndex++] = 0;
            chan_buf[channelBuffer][pIndex++] = 0;
            chan_buf[channelBuffer][pIndex++] = 0;

            //buf_len[0] = (int)(p - chan_buf[0]);
            buf_len[0] = pIndex;

            return (byte)C64StatusCode.ST_OK;
        }


        
        private void AllocateChannelBuffer(int channel, int size)
        {
            //chan_buf_alloc[channel] = new byte[size];
            //chan_buf[channel] = chan_buf_alloc[channel];

            chan_buf[channel] = new byte[size];

        }


        private void DeallocateChannelBuffer(int channel)
        {
            //chan_buf_alloc[channel] = null;
            chan_buf[channel] = null;
        }

        // TODO
        byte open_direct(int channel, byte[] filename)
        {
        //    int buf = -1;

        //    if (filename[1] == 0)
        //        buf = alloc_buffer(-1);
        //    else
        //        if ((filename[1] >= '0') && (filename[1] <= '3') && (filename[2] == 0))
        //            buf = alloc_buffer(filename[1] - '0');

        //    if (buf == -1)
        //    {
        //        set_error(ErrorCode1541.ERR_NOCHANNEL);
        //        return (byte)C64StatusCode.ST_OK;
        //    }

        //    unsafe
        //    {
        //        // The buffers are in the 1541 RAM at $300 and are 256 bytes each
        //        chan_buf[channel] = buf_ptr[channel] = (byte*)ram + 0x300 + (buf << 8);
        //        chan_mode[channel] = ChannelMode.CHMOD_DIRECT;
        //        chan_buf_num[channel] = buf;

        //        // Store actual buffer number in buffer
        //        *chan_buf[channel] = (byte)(buf + '0');
        //        buf_len[channel] = 1;
        //    }

            return (byte)C64StatusCode.ST_OK;
        }

        void close_all_channels()
        {
            for (int i = 0; i < 15; i++)
                Close(i);

            cmd_len = 0;
        }

        // TODO
        void execute_command(byte[] command)
        {
            UInt16 adr;
            int len;

            switch ((char)command[0])
            {
                case 'B':
                    if (command[1] != '-')
                        set_error(ErrorCode1541.ERR_SYNTAX30);
                    else
                        switch ((char)command[2])
                        {
                            case 'R':
                                block_read_cmd(command.CopySubset(3));
                                break;

                            case 'P':
                                buffer_ptr_cmd(command.CopySubset(3));
                                break;

                            case 'A':
                            case 'F':
                            case 'W':
                                set_error(ErrorCode1541.ERR_WRITEPROTECT);
                                break;

                            default:
                                set_error(ErrorCode1541.ERR_SYNTAX30);
                                break;
                        }
                    break;

                case 'M':
                    if (command[1] != '-')
                        set_error(ErrorCode1541.ERR_SYNTAX30);
                    else
                        switch ((char)command[2])
                        {
                            case 'R':
                                // TODO

                                //adr = (UInt16)(((byte)command[4] << 8) | ((byte)command[3]));
                                //error_ptr = (byte*)((byte*)_ram + (adr & 0x07ff));
                                //if ((error_len = (byte)command[5]) == 0)
                                //    error_len = 1;
                                adr = (UInt16)(((byte)command[4] << 8) | ((byte)command[3]));
                                //TODO

                                break;

                            case 'W':
                                adr = (UInt16)(((byte)command[4] << 8) | ((byte)command[3]));
                                len = (byte)command[5];
                                for (int i = 0; i < len; i++)
                                    _ram[adr + i] = (byte)command[i + 6];
                                break;

                            default:
                                set_error(ErrorCode1541.ERR_SYNTAX30);
                                break;
                        }
                    break;

                case 'I':
                    close_all_channels();

                    byte[] buffer = Bam.AllocateBuffer();
                    read_sector(18, 0, buffer);
                    bam = new Bam(buffer);

                    set_error(ErrorCode1541.ERR_OK);
                    break;

                case 'U':
                    switch (command[1] & 0x0f)
                    {
                        case 1:		// U1/UA: Block-Read
                            block_read_cmd(command.CopySubset(2));
                            break;

                        case 2:		// U2/UB: Block-Write
                            set_error(ErrorCode1541.ERR_WRITEPROTECT);
                            break;

                        case 10:	// U:/UJ: Reset
                            Reset();
                            break;

                        default:
                            set_error(ErrorCode1541.ERR_SYNTAX30);
                            break;
                    }
                    break;

                case 'G':
                    if (command[1] != ':')
                        set_error(ErrorCode1541.ERR_SYNTAX30);
                    else
                        chd64_cmd(command.CopySubset(2));
                    break;

                case 'C':
                case 'N':
                case 'R':
                case 'S':
                case 'V':
                    set_error(ErrorCode1541.ERR_WRITEPROTECT);
                    break;

                default:
                    set_error(ErrorCode1541.ERR_SYNTAX30);
                    break;
            }
        }

        void block_read_cmd(byte[] command)
        {
            int channel = 0, drvnum = 0, track = 0, sector = 0;

            if (parse_bcmd(command, ref channel, ref drvnum, ref track, ref sector))
            {
                if (chan_mode[channel] == ChannelMode.CHMOD_DIRECT)
                {
                    //read_sector(track, sector, buf_ptr[channel] = chan_buf[channel]);
                    read_sector(track, sector, chan_buf[channel]);
                    buf_len[channel] = 256;
                    set_error(ErrorCode1541.ERR_OK);
                }
                else
                    set_error(ErrorCode1541.ERR_NOCHANNEL);
            }
            else
                set_error(ErrorCode1541.ERR_SYNTAX30);
        }

        // TODO
        void buffer_ptr_cmd(byte[] command)
        {
        //    int channel = 0, pointer = 0, i = 0;

        //    if (parse_bcmd(command, ref channel, ref pointer, ref i, ref i))
        //    {
        //        if (chan_mode[channel] == ChannelMode.CHMOD_DIRECT)
        //        {
        //            buf_ptr[channel] = chan_buf[channel] + pointer;
        //            buf_len[channel] = 256 - pointer;
        //            set_error(ErrorCode1541.ERR_OK);
        //        }
        //        else
        //            set_error(ErrorCode1541.ERR_NOCHANNEL);
        //    }
        //    else
        //        set_error(ErrorCode1541.ERR_SYNTAX30);
        }

#if false
        //unsafe bool parse_bcmd(byte* cmd, ref int arg1, ref int arg2, ref int arg3, ref int arg4)
        //{
        //    int i;

        //    if (*cmd == ':') cmd++;

        //    // Read four parameters separated by space, cursor right or comma
        //    while (*cmd == ' ' || *cmd == 0x1d || *cmd == 0x2c) cmd++;
        //    if (*cmd == 0) return false;

        //    i = 0;
        //    while (*cmd >= 0x30 && *cmd < 0x40)
        //    {
        //        i *= 10;
        //        i += *cmd++ & 0x0f;
        //    }
        //    arg1 = i & 0xff;

        //    while (*cmd == ' ' || *cmd == 0x1d || *cmd == 0x2c) cmd++;
        //    if (*cmd == 0) return false;

        //    i = 0;
        //    while (*cmd >= 0x30 && *cmd < 0x40)
        //    {
        //        i *= 10;
        //        i += *cmd++ & 0x0f;
        //    }
        //    arg2 = i & 0xff;

        //    while (*cmd == ' ' || *cmd == 0x1d || *cmd == 0x2c) cmd++;
        //    if (*cmd == 0) return false;

        //    i = 0;
        //    while (*cmd >= 0x30 && *cmd < 0x40)
        //    {
        //        i *= 10;
        //        i += *cmd++ & 0x0f;
        //    }
        //    arg3 = i & 0xff;

        //    while (*cmd == ' ' || *cmd == 0x1d || *cmd == 0x2c) cmd++;
        //    if (*cmd == 0) return false;

        //    i = 0;
        //    while (*cmd >= 0x30 && *cmd < 0x40)
        //    {
        //        i *= 10;
        //        i += *cmd++ & 0x0f;
        //    }
        //    arg4 = i & 0xff;

        //    return true;
        //}

#endif



        // TODO
        bool parse_bcmd(byte[] command, ref int arg1, ref int arg2, ref int arg3, ref int arg4)
        {
            int i;
            byte[] cmd;
            int cmdIndex = 0;

            if (command[0] == ':')
                cmd = command.CopySubset(1);
            else
                cmd = command.CopySubset(0);
            

            // Read four parameters separated by space, cursor right or comma
            while (cmd[cmdIndex] == ' ' || cmd[cmdIndex] == 0x1d || cmd[cmdIndex] == 0x2c) 
                cmdIndex++;

            if (cmd[cmdIndex] == 0)
                return false;

        //    i = 0;
        //    while (*cmd >= 0x30 && *cmd < 0x40)
        //    {
        //        i *= 10;
        //        i += *cmd++ & 0x0f;
        //    }
        //    arg1 = i & 0xff;

            i = 0;
            while (cmd[cmdIndex] >= 0x30 && cmd[cmdIndex] < 0x40)
            {
                i *= 10;
                i += cmd[cmdIndex++] & 0x0f;
            }
            arg1 = i & 0xff;



        //    while (*cmd == ' ' || *cmd == 0x1d || *cmd == 0x2c) cmd++;
        //    if (*cmd == 0) return false;

            while (cmd[cmdIndex] == ' ' || cmd[cmdIndex] == 0x1d || cmd[cmdIndex] == 0x2c)
                cmdIndex++;
            if (cmd[cmdIndex] == 0)
                return false;



        //    i = 0;
        //    while (*cmd >= 0x30 && *cmd < 0x40)
        //    {
        //        i *= 10;
        //        i += *cmd++ & 0x0f;
        //    }
        //    arg2 = i & 0xff;
            i = 0;
            while (cmd[cmdIndex] >= 0x30 && cmd[cmdIndex] < 0x40)
            {
                i *= 10;
                i += cmd[cmdIndex++] & 0x0f;
            }
            arg2 = i & 0xff;





        //    while (*cmd == ' ' || *cmd == 0x1d || *cmd == 0x2c) cmd++;
        //    if (*cmd == 0) return false;
            while (cmd[cmdIndex] == ' ' || cmd[cmdIndex] == 0x1d || cmd[cmdIndex] == 0x2c)
                cmdIndex++;
            if (cmd[cmdIndex] == 0)
                return false;



        //    i = 0;
        //    while (*cmd >= 0x30 && *cmd < 0x40)
        //    {
        //        i *= 10;
        //        i += *cmd++ & 0x0f;
        //    }
        //    arg3 = i & 0xff;
            i = 0;
            while (cmd[cmdIndex] >= 0x30 && cmd[cmdIndex] < 0x40)
            {
                i *= 10;
                i += cmd[cmdIndex++] & 0x0f;
            }
            arg3 = i & 0xff;




        //    while (*cmd == ' ' || *cmd == 0x1d || *cmd == 0x2c) cmd++;
        //    if (*cmd == 0) return false;
            while (cmd[cmdIndex] == ' ' || cmd[cmdIndex] == 0x1d || cmd[cmdIndex] == 0x2c)
                cmdIndex++;
            if (cmd[cmdIndex] == 0)
                return false;


        //    i = 0;
        //    while (*cmd >= 0x30 && *cmd < 0x40)
        //    {
        //        i *= 10;
        //        i += *cmd++ & 0x0f;
        //    }
        //    arg4 = i & 0xff;
            i = 0;
            while (cmd[cmdIndex] >= 0x30 && cmd[cmdIndex] < 0x40)
            {
                i *= 10;
                i += cmd[cmdIndex++] & 0x0f;
            }
            arg4 = i & 0xff;


            return true;
        }

        // TODO
        void chd64_cmd(byte[] d64name)
        {
        //    using (BytePtr str = new BytePtr(IEC.NAMEBUF_LENGTH))
        //    {
        //        byte* p = str;

        //        // Convert .d64 file name
        //        for (int i = 0; i < IEC.NAMEBUF_LENGTH && (*p++ = conv_from_64(*d64name++, false)) != 0; i++) ;

        //        close_all_channels();

        //        // G:. resets the .d64 file name to its original setting
        //        if (str[0] == '.' && str[1] == 0)
        //            open_close_d64_file(orig_d64_name);
        //        else
        //            open_close_d64_file(str.ToString());

        //        // Read BAM
        //        read_sector(18, 0, (byte*)bam);
        //    }
        }

        int alloc_buffer(int want)
        {
            if (want == -1)
            {
                for (want = 3; want >= 0; want--)
                    if (buf_free[want])
                    {
                        buf_free[want] = false;
                        return want;
                    }
                return -1;
            }

            if (want < 4)
                if (buf_free[want])
                {
                    buf_free[want] = false;
                    return want;
                }
                else
                    return -1;
            else
                return -1;
        }

        void free_buffer(int buf)
        {
            buf_free[buf] = true;
        }

        bool read_sector(int track, int sector, byte[] buffer)
        {
            int offset;

            // Convert track/sector to byte offset in file
            if ((offset = offset_from_ts(track, sector)) < 0)
            {
                set_error(ErrorCode1541.ERR_ILLEGALTS);
                return false;
            }

            if (the_file == null)
            {
                set_error(ErrorCode1541.ERR_NOTREADY);
                return false;
            }

            the_file.Seek(offset + image_header, SeekOrigin.Begin);
            byte[] tmp = new byte[256];
            the_file.Read(tmp, 0, 256);

//            Marshal.Copy(tmp, 0, (IntPtr)buffer, 256);
            for (int i = 0; i < 256; i++)
            {
                buffer[i] = tmp[i];
            }
            
            return true;
        }

        int offset_from_ts(int track, int sector)
        {
            if ((track < 1) || (track > NUM_TRACKS) || (sector < 0) || (sector >= num_sectors[track]))
                return -1;

            return (sector_offset[track] + sector) << 8;
        }

        byte conv_from_64(byte c, bool map_slash)
        {
            if ((c >= 'A') && (c <= 'Z') || (c >= 'a') && (c <= 'z'))
                return (byte)(c ^ 0x20);

            if ((c >= 0xc1) && (c <= 0xda))
                return (byte)(c ^ 0x80);

            if ((c == '/') && map_slash && GlobalPrefs.ThePrefs.MapSlash)
                return (byte)'\\';

            return c;
        }

        #endregion


        #region IDisposable Members

        bool disposed = false;

        ~D64Drive()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                }

                disposed = true;
            }
        }

        #endregion
    }


    //     Bytes:$00-01: Track/Sector location of the first directory sector (should
    //                be set to 18/1 but it doesn't matter, and don't trust  what
    //                is there, always go to 18/1 for first directory entry)
    //            02: Disk DOS version type (see note below)
    //                  $41 ("A")
    //            03: Unused
    //         04-8F: BAM entries for each track, in groups  of  four  bytes  per
    //                track, starting on track 1 (see below for more details)
    //         90-9F: Disk Name (padded with $A0)
    //         A0-A1: Filled with $A0
    //         A2-A3: Disk ID
    //            A4: Usually $A0
    //         A5-A6: DOS type, usually "2A"
    //         A7-AA: Filled with $A0
    //         AB-FF: Normally unused ($00), except for 40 track extended format,
    //                see the following two entries:
    //         AC-BF: DOLPHIN DOS track 36-40 BAM entries (only for 40 track)
    //         C0-D3: SPEED DOS track 36-40 BAM entries (only for 40 track)

    //Note: The BAM entries for SPEED, DOLPHIN and  ProLogic  DOS  use  the  same
    //      layout as standard BAM entries.


    // BAM structure
    [StructLayout(LayoutKind.Sequential)]
    class Bam
    {
        public byte dir_track;		// Track...
        public byte dir_sector;		// ...and sector of first directory block
        public sbyte fmt_type;		// Format type
        public sbyte pad0;
        public byte[] bitmap = new byte[4 * 35];	// Sector allocation
        public byte[] disk_name = new byte[16];	// Disk name
        public byte[] pad_name = new byte[2];   // padding
        public byte[] id = new byte[2];			// Disk ID
        public byte pad1;
        public byte[] fmt_char = new byte[2];	// Format characters
        public sbyte[] pad2 = new sbyte[4];
        public sbyte[] pad3 = new sbyte[85];

        public Bam(byte[] bytes)
        {
            int current = 0;

            dir_track = bytes[current++];
            dir_sector = bytes[current++];

            // override dir_track and dir_sector per instructions on D64 format site
            dir_track = 18;
            dir_sector = 1;

            fmt_type = (sbyte)bytes[current++];
            pad0 = (sbyte)bytes[current++];

            current = bitmap.CopyFrom(bytes, current);

            current = disk_name.CopyFrom(bytes, current);
            pad_name[0] = bytes[current++];
            pad_name[1] = bytes[current++];

            id[0] = bytes[current++];   // 162
            id[1] = bytes[current++];   // 163
            
            pad1 = bytes[current++]; // 164

            fmt_char[0] = bytes[current++]; //165
            fmt_char[1] = bytes[current++];

            pad2[0] = (sbyte)bytes[current++];
            pad2[1] = (sbyte)bytes[current++];
            pad2[2] = (sbyte)bytes[current++];
            pad2[3] = (sbyte)bytes[current++];

            current = pad3.CopyFrom(bytes, current);

            //for (int i = 0; i < 85; i++)
            //{
            //    pad3[i] = (sbyte)bytes[current++];
            //}
            

        }


        public static byte[] AllocateBuffer()
        {
            return new byte[256];
        }



    };

    // Structure information from http://unusedino.de/ec64/technical/formats/d64.html
    //
    // Bytes: $00-1F: First directory entry
    //          00-01: Track/Sector location of next directory sector ($00 $00 if
    //                 not the first entry in the sector)
    //             02: File type.
    //                 Typical values for this location are:
    //                   $00 - Scratched (deleted file entry)
    //                    80 - DEL
    //                    81 - SEQ
    //                    82 - PRG
    //                    83 - USR
    //                    84 - REL
    //                 Bit 0-3: The actual filetype
    //                          000 (0) - DEL
    //                          001 (1) - SEQ
    //                          010 (2) - PRG
    //                          011 (3) - USR
    //                          100 (4) - REL
    //                          Values 5-15 are illegal, but if used will produce
    //                          very strange results. The 1541 is inconsistent in
    //                          how it treats these bits. Some routines use all 4
    //                          bits, others ignore bit 3,  resulting  in  values
    //                          from 0-7.
    //                 Bit   4: Not used
    //                 Bit   5: Used only during SAVE-@ replacement
    //                 Bit   6: Locked flag (Set produces ">" locked files)
    //                 Bit   7: Closed flag  (Not  set  produces  "*", or "splat"
    //                          files)
    //          03-04: Track/sector location of first sector of file
    //          05-14: 16 character filename (in PETASCII, padded with $A0)
    //          15-16: Track/Sector location of first side-sector block (REL file
    //                 only)
    //             17: REL file record length (REL file only, max. value 254)
    //          18-1D: Unused (except with GEOS disks)
    //          1E-1F: File size in sectors, low/high byte  order  ($1E+$1F*256).
    //                 The approx. filesize in bytes is <= #sectors * 254
    //          20-3F: Second dir entry. From now on the first two bytes of  each
    //                 entry in this sector  should  be  $00  $00,  as  they  are
    //                 unused.
    //          40-5F: Third dir entry
    //          60-7F: Fourth dir entry
    //          80-9F: Fifth dir entry
    //          A0-BF: Sixth dir entry
    //          C0-DF: Seventh dir entry
    //          E0-FF: Eighth dir entry


    // Directory entry structure
    //[StructLayout(LayoutKind.Sequential)]
    class DirEntry
    {
        public byte type;			// File type
        public byte track;			// Track...
        public byte sector;			// ...and sector of first data block
        public byte[] name = new byte[16];		// File name
        public byte side_track;		// Track...
        public byte side_sector;	// ...and sector of first side sector
        public byte rec_len;		// Record length
        public sbyte[] pad0 = new sbyte[4];
        public byte ovr_track;		// Track...
        public byte ovr_sector;		// ...and sector on overwrite
        public byte num_blocks_l;	// Number of blocks, LSB
        public byte num_blocks_h;	// Number of blocks, MSB
        public sbyte[] pad1 = new sbyte[2];

        public DirEntry(byte[] bytes)
        {
            int current = 0;

            type = bytes[current++];
            track = bytes[current++];
            sector = bytes[current++];

            current = name.CopyFrom(bytes, current);

            side_track = bytes[current++];
            side_sector = bytes[current++];
            rec_len = bytes[current++];

            current = pad0.CopyFrom(bytes, current);

            ovr_track = bytes[current++];
            ovr_sector = bytes[current++];
            num_blocks_l = bytes[current++];
            num_blocks_h = bytes[current++];

            current = pad1.CopyFrom(bytes, current);

        }

    } ;

    // Directory block structure
    [StructLayout(LayoutKind.Sequential)]
    class Directory
    {
        //public byte[] padding = new byte[2];		// Keep DirEntry word-aligned
        public byte next_track;
        public byte next_sector;
        //public byte[] entry = new byte[8 * 32];   // array of 8 DirEntry structs (sizeof(DirEntry) = 32)

        public DirEntry[] Entries = new DirEntry[8];    // array of 8 DirEntry structs (sizeof(DirEntry) = 32)


        public Directory(byte[] bytes)
        {
            int current = 0;

            //current = padding.CopyFrom(bytes, current) + 1;

            next_track = bytes[current++];
            next_sector = bytes[current++];

            //current = entry.CopyFrom(bytes, current) + 1;

            for (int i = 0; i < 8; i++)
            {
                byte[] entry = new byte[32];

                Array.Copy(bytes, current + i * 32, entry, 0, 32);
                Entries[i] = new DirEntry(entry);
            }

        }

        public static byte[] AllocateBuffer()
        {
            return new byte[258];
        }
    };

    // Channel modes (IRC users listen up :-)
    public enum ChannelMode
    {
        CHMOD_FREE,			// Channel free
        CHMOD_COMMAND,		// Command/error channel
        CHMOD_DIRECTORY,	// Reading directory
        CHMOD_FILE,			// Sequential file open
        CHMOD_DIRECT		// Direct buffer access ('#')
    };

    // Access modes
    public enum FileAccessMode
    {
        FMODE_READ, 
        FMODE_WRITE, 
        FMODE_APPEND
    };

    // File types
    public enum FileType
    {
        FTYPE_PRG, 
        FTYPE_SEQ, 
        FTYPE_USR, 
        FTYPE_REL
    };


    // helper class added by PMB
    // wouldn't be needed if I had time to rewrite the D64 drive code from scratch
    public class C64InternalFileSpec
    {
        public FileType FileType { get; private set; }
        public FileAccessMode AccessMode { get; private set; }
        public byte[] Name { get; private set; }

        public C64InternalFileSpec(byte[] fileName)
        {
            Parse(fileName);
        }

        private enum ParseState
        {
            Preamble,
            FileName,
            Attributes
        }

        private void Parse(byte[] fileName)
        {
            ParseState state = ParseState.Preamble;
            byte[] nameBuffer = new byte[fileName.Length];
            int nameIndex = 0;

            if (Array.IndexOf<byte>(fileName, (byte)':') >= 0)
                state = ParseState.Preamble;
            else
                state = ParseState.FileName;

            for (int i = 0; i < fileName.Length; i++)
            {
                char ch = (char)fileName[i];

                switch (state)
                {
                    case ParseState.Preamble:
                        if (ch == ':')
                            state = ParseState.FileName;
                        break;

                    case ParseState.FileName:
                        if (ch == ',')
                            state = ParseState.Attributes;
                        else
                            nameBuffer[nameIndex++] = (byte)ch;
                        break;

                    case ParseState.Attributes:
                        switch (ch)
                        {
                            case 'P':
                                FileType = FileType.FTYPE_PRG;
                                break;
                            case 'S':
                                FileType = FileType.FTYPE_SEQ;
                                break;
                            case 'U':
                                FileType = FileType.FTYPE_USR;
                                break;
                            case 'L':
                                FileType = FileType.FTYPE_REL;
                                break;

                            case 'R':
                                AccessMode = FileAccessMode.FMODE_READ;
                                break;
                            case 'W':
                                AccessMode = FileAccessMode.FMODE_WRITE;
                                break;
                            case 'A':
                                AccessMode = FileAccessMode.FMODE_APPEND;
                                break;
                        }
                        break;

                }


                // copy the filename over
                Name = new byte[nameIndex];
                Array.Copy(nameBuffer, Name, nameIndex);

            }
        }
    }


}
