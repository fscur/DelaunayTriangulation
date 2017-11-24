using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    public class DoubleComparer : IComparer<double>
    {
        public int Compare(double v0, double v1)
        {
            if (TriangleLib.Compare.Less(v0, v1))
                return -1;
            else if (TriangleLib.Compare.Greater(v0, v1))
                return 1;

            return 0;
        }
    }
}
