using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SC2BB2
{
    struct Target
    {
        public string Name;
        public int Count;

        public Target(string t, int count)
        {
            Name = t;
            Count = count;
        }
    }
}
