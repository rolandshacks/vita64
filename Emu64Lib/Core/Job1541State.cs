using System;

namespace C64Lib.Core
{
    public class Job1541State
    {
        public int current_halftrack;
        public UInt32 gcr_ptr;
        public bool write_protected;
        public bool disk_changed;
    }
}
