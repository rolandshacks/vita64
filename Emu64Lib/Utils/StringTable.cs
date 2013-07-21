
using System;
using System.Collections.Generic;
namespace C64Lib.Utils
{
    public class StringTable : List<String>
    {

        public int? CurrentItemIndex { get; set; }

        public string CurrentItem
        {
            get
            {
                if (CurrentItemIndex != null)
                    return this[(int)CurrentItemIndex];
                else
                    return null;
            }
        }

    }
}
