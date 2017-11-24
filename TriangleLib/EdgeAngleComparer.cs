using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    public class EdgeAngleComparer : IComparer<Edge>
    {
        private Vertex _pivot;

        public EdgeAngleComparer(Vertex pivot)
        {
            _pivot = pivot;
        }
        public int Compare(Edge e0, Edge e1)
        {
            var a0 = Edge.GetRelativeAngleToVertex(e0, _pivot);
            var a1 = Edge.GetRelativeAngleToVertex(e1, _pivot);

            if (TriangleLib.Compare.Less(a0, a1, TriangleLib.Compare.TOLERANCE))
                return -1;
            else if (TriangleLib.Compare.Greater(a0, a1, TriangleLib.Compare.TOLERANCE))
                return 1;
            else
            {
                var l0 = e0.Length;
                var l1 = e1.Length;

                if (TriangleLib.Compare.Less(l0, l1, TriangleLib.Compare.TOLERANCE))
                    return -1;
                else if (TriangleLib.Compare.Greater(l0, l1, TriangleLib.Compare.TOLERANCE))
                    return 1;
            }

            return 0;
        }
    }
}
