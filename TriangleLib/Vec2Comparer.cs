using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    public class Vec2EqualityComparer : IEqualityComparer<Vec2>
    {
        public bool Equals(Vec2 v0, Vec2 v1)
        {
            return v0 == v1;
        }

        public int GetHashCode(Vec2 v)
        {
            return v.GetHashCode();
        }
    }

    public class Vec2Comparer : IComparer<Vec2>
    {
        public Vec2Comparer()
        {
        }
        public int Compare(Vec2 v0, Vec2 v1)
        {
            if (TriangleLib.Compare.Less(v0.X, v1.X))
                return -1;
            else if (TriangleLib.Compare.Greater(v0.X, v1.X))
                return 1;
            else if (TriangleLib.Compare.Less(v0.Y, v1.Y))
                return -1;
            else if (TriangleLib.Compare.Greater(v0.Y, v1.Y))
                return 1;

            return 0;
        }
    }
}
