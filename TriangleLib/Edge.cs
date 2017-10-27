using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    public class Edge
    {
        private Vertex _v0;
        private Vertex _v1;
        private Vec2 _direction;
        private double _length;
        private Triangle _t0;

        public Triangle T0;
        public Triangle T1;

        public double Length { get { return _length; } }

        public Vec2 Direction { get { return _direction; } }

        public Vertex V0 { get { return _v0; } }

        public Vertex V1 { get { return _v1; } }

        public Vec2 MidPoint
        {
            get { return (V0.Position + V1.Position) * 0.5; }
        }

        public Edge(Vertex v0, Vertex v1)
        {
            _v0 = v0;
            _v1 = v1;
            _length = Vec2.Length(_v1.Position - _v0.Position);
            _direction = (_v1.Position - _v0.Position) / _length;
        }
        
        public static bool Intersects(Edge e0, Edge e1)
        {
            if (e0._v0.Position == e1._v0.Position ||
                e0._v0.Position == e1._v1.Position ||
                e0._v1.Position == e1._v0.Position ||
                e0._v1.Position == e1._v1.Position)
                return false;

            var a = e0._v0.Position;
            var b = e0._v1.Position;
            var c = e1._v0.Position;
            var d = e1._v1.Position;

            var ca = Vec2.Cross(c - a, d - a);
            var cb = Vec2.Cross(c - b, d - b);
            var cc = Vec2.Cross(a - c, b - c);
            var cd = Vec2.Cross(a - d, b - d);

            var ccwa = Compare.LessOrEqual(ca, 0);
            var ccwb = Compare.LessOrEqual(cb, 0);

            var ccwc = Compare.LessOrEqual(cc, 0);
            var ccwd = Compare.LessOrEqual(cd, 0);

            return !(ccwa == ccwb || ccwc == ccwd);
        }

        public Edge GetOppositeEdge()
        {
            return new Edge(V1, V0);
        }

        public override string ToString()
        {
            return "[" + _v0 + "; " + _v1 + "]";
        }

        public static double GetRelativeAngleToVertex(Edge edge, Vertex vertex)
        {
            Vertex testVertex = null;

            if (vertex == edge._v0)
                testVertex = edge._v1;
            else if (vertex == edge._v1)
                testVertex = edge._v0;
            else
                throw new InvalidOperationException("Input vertex should be one of the input edge.");

            var direction = testVertex.Position - vertex.Position;
            return (Math.Atan2(direction.Y, direction.X) + 2.0 * Math.PI) % (2.0 * Math.PI);
        }
    }
}
