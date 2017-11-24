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

        List<Triangle> _triangles;
        public List<Triangle> Triangles
        {
            get { return _triangles; }
        }

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
            _triangles = new List<Triangle>();
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

            var ccwa = Compare.LessOrEqual(ca, 0, Compare.TOLERANCE);
            var ccwb = Compare.LessOrEqual(cb, 0, Compare.TOLERANCE);

            var ccwc = Compare.LessOrEqual(cc, 0, Compare.TOLERANCE);
            var ccwd = Compare.LessOrEqual(cd, 0, Compare.TOLERANCE);

            return !(ccwa == ccwb || ccwc == ccwd);
        }

        public Edge InverseEdge()
        {
            return new Edge(V1, V0);
        }

        public Vertex FindOppositeVertex(Triangle triangle)
        {
            if (Triangles.Count < 1)
                return null;

            var t = triangle;

            if (Triangles.Count == 2)
                t = Triangles.FirstOrDefault(t0 => t0 != triangle && t0.E0 != null && t0.E1 != null && t0.E2 != null);

            if (t == null)
                return null;

            if ((t.V0 == V0 && t.V1 == V1) || (t.V0 == V1 && t.V1 == V0))
                return t.V2;
            else if ((t.V1 == V0 && t.V2 == V1) || (t.V1 == V1 && t.V2 == V0))
                return t.V0;
            else if ((t.V2 == V0 && t.V0 == V1) || (t.V2 == V1 && t.V0 == V0))
                return t.V1;

            return null;
        }

        public static bool IsDelaunay(Edge edge)
        {
            var triangles = edge.Triangles.Where(t => !Triangle.IsDegenerate(t)).ToList();

            if (triangles.Count < 2)
                return true;

            var t0 = triangles[0];

            Vec2 p = edge.FindOppositeVertex(t0).Position;

            //if (t0 == null)
            //{
            //    t0 = triangles[0];
            //    p = edge.FindOppositeVertex(t0).Position;
            //    return Compare.Greater(Vec2.Length(p - edge.MidPoint), Vec2.Length(edge.V0.Position - edge.MidPoint));
            //}

            //p = edge.FindOppositeVertex(t0).Position;

            return !Triangle.CircumcircleContainsPoint(t0, p);
        }

        public Edge FlipEdge()
        {
            Vertex v0 = FindOppositeVertex(_triangles[0]);
            Vertex v1 = FindOppositeVertex(_triangles[1]);

            return new Edge(v0, v1);
        }

        public override string ToString()
        {
            return "[" + _v0 + "; " + _v1 + "]";
        }

        public static double GetRelativeAngleToVertex(Edge edge, Vertex vertex)
        {
            Vertex testVertex = vertex.Position == edge._v0.Position ? edge._v1 : edge._v0;
            var direction = testVertex.Position - vertex.Position;
            return (Math.Atan2(direction.Y, direction.X) + 2.0 * Math.PI) % (2.0 * Math.PI);
        }

        internal void RemoveDegenerateTriangles()
        {
            Triangles.RemoveAll(t => t.E0 == null || t.E2 == null || t.E2 == null);
        }

        public struct EdgeIntersection
        {
            public bool Intersects;
            public Edge E0;
            public Edge E1;
            public double S;
            public double T;
            public Vertex Vertex;
        }

        public static EdgeIntersection Intersect(Edge e0, Edge e1)
        {
            var x0 = e0.V0.Position.X;
            var x1 = e0.V1.Position.X;
            var y0 = e0.V0.Position.Y;
            var y1 = e0.V1.Position.Y;

            var x2 = e1.V0.Position.X;
            var x3 = e1.V1.Position.X;
            var y2 = e1.V0.Position.Y;
            var y3 = e1.V1.Position.Y;

            var x10 = x1 - x0;
            var y10 = y1 - y0;
            var x32 = x3 - x2;
            var y32 = y3 - y2;

            var det = x32 * y10 - x10 * y32;

            if (Compare.AlmostEqual(det, 0.0, Compare.TOLERANCE))
                return new EdgeIntersection() { Intersects = false };

            var x20 = x2 - x0;
            var y20 = y2 - y0;

            var t = (x32 * y20 - x20 * y32) / det;
            var s = (x10 * y20 - x20 * y10) / det;

            var a = e0.V0.Position + t * (e0.V1.Position - e0.V0.Position);
            var b = e1.V0.Position + s * (e1.V1.Position - e1.V0.Position);

            Vertex vertex = null;

            if (Compare.AlmostEqual(s, 0.0, Compare.TOLERANCE))
                vertex = e0.V0;
            else if (Compare.AlmostEqual(s, 1.0, Compare.TOLERANCE))
                vertex = e0.V1;
            else if (Compare.AlmostEqual(t, 0.0, Compare.TOLERANCE))
                vertex = e1.V0;
            else if (Compare.AlmostEqual(t, 1.0, Compare.TOLERANCE))
                vertex = e1.V1;
            else
                vertex = new Vertex(e0.V0.Position + t * (e0.V1.Position - e0.V0.Position));

            return new EdgeIntersection()
            {
                Intersects = true,
                E0 = e0,
                E1 = e1,
                S = s,
                T = t,
                Vertex = vertex
            };
        }
    }
}
