using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trianglex
{
    public static class Compare
    {
        static readonly double EPSILON = 0.5E-9;
        static double RelativeError(double a, double b)
        {
            return Math.Abs(a - b) / Math.Max(Math.Abs(a), Math.Abs(b));
        }
        public static bool AlmostEqual(double a, double b)
        {
            return a == b || RelativeError(a, b) < EPSILON;
        }

        public static bool GreaterOrEqual(double a, double b)
        {
            return a > b || AlmostEqual(a, b);
        }

        public static bool LessOrEqual(double a, double b)
        {
            return a < b || AlmostEqual(a, b);
        }

        public static bool Less(double a, double b)
        {
            return !AlmostEqual(a, b) && a < b;
        }

        public static bool Greater(double a, double b)
        {
            return !AlmostEqual(a, b) && a > b;
        }
    }

    public class Vec2
    {
        public double X;
        public double Y;

        public PointF ToPointF()
        {
            return new PointF((float)X, (float)Y);
        }

        public override string ToString()
        {
            return "(" + X + "; " + Y + ")";
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            Vec2 p = (Vec2)obj;
            return Compare.AlmostEqual(X, p.X) && Compare.AlmostEqual(Y, p.Y);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Vec2 a, Vec2 b)
        {
            if (object.ReferenceEquals(a, null))
                return object.ReferenceEquals(b, null);

            return a.Equals(b);
        }

        public static bool operator !=(Vec2 a, Vec2 b)
        {
            return !(a == b);
        }

        public static Vec2 operator -(Vec2 a, Vec2 b)
        {
            return new Vec2() { X = a.X - b.X, Y = a.Y - b.Y };
        }

        public static Vec2 operator -(Vec2 a)
        {
            return new Vec2() { X = -a.X, Y = -a.Y };
        }

        public static Vec2 operator +(Vec2 a, Vec2 b)
        {
            return new Vec2() { X = a.X + b.X, Y = a.Y + b.Y };
        }

        public static Vec2 operator *(Vec2 a, double b)
        {
            return new Vec2() { X = a.X * b, Y = a.Y * b };
        }

        public static Vec2 operator *(double a, Vec2 b)
        {
            return new Vec2() { X = a * b.X, Y = a * b.Y };
        }

        public static Vec2 operator /(Vec2 a, double b)
        {
            return new Vec2() { X = a.X / b, Y = a.Y / b };
        }

        public static double Length(Vec2 a)
        {
            var dx = System.Math.Pow(a.X, 2.0);
            var dy = System.Math.Pow(a.Y, 2.0);

            return System.Math.Sqrt(dx + dy);
        }

        public static double SquaredLength(Vec2 a)
        {
            var dx = System.Math.Pow(a.X, 2.0);
            var dy = System.Math.Pow(a.Y, 2.0);

            return dx + dy;
        }

        public static Vec2 Normalize(Vec2 a)
        {
            return a / Length(a);
        }

        public static double Dot(Vec2 a, Vec2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public static double Cross(Vec2 a, Vec2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }
    }

    public class PivotVertexEdgeComparer : IComparer<Edge>
    {
        private Vertex _pivot;

        public PivotVertexEdgeComparer(Vertex pivot)
        {
            _pivot = pivot;
        }
        public int Compare(Edge e0, Edge e1)
        {
            var a0 = Edge.GetRelativeAngleToVertex(e0, _pivot);
            var a1 = Edge.GetRelativeAngleToVertex(e1, _pivot);

            if (Trianglex.Compare.Less(a0, a1))
                return -1;
            else if (Trianglex.Compare.Greater(a0, a1))
                return 1;
            else
            {
                var l0 = e0.Length;
                var l1 = e1.Length;

                if (Trianglex.Compare.Less(l0, l1))
                    return -1;
                else if (Trianglex.Compare.Greater(l0, l1))
                    return 1;
            }

            return 0;
        }
    }

    public class Vertex
    {
        private IComparer<Edge> _edgeComparer;

        public Vec2 Position;

        public List<Edge> Edges;

        public Vertex(Vec2 position)
        {
            Position = position;
            Edges = new List<Edge>();
            _edgeComparer = new PivotVertexEdgeComparer(this);
        }

        public override bool Equals(Object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;

            Vertex p = (Vertex)obj;

            foreach (var edge in p.Edges)
                if (!Edges.Contains(edge))
                    return false;

            return Position == p.Position;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        public static bool operator ==(Vertex a, Vertex b)
        {
            if (object.ReferenceEquals(a, null))
                return object.ReferenceEquals(b, null);

            return a.Equals(b);
        }

        public static bool operator !=(Vertex a, Vertex b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return Position.ToString();
        }

        public void AddEdge(Edge edge)
        {
            Edges.Add(edge);
            Edges.Sort(_edgeComparer);
        }

        public void RemoveEdge(Edge edge)
        {
            Edges.Remove(edge);
            Edges.Sort(_edgeComparer);
        }
    }

    public class Edge
    {
        private Vertex _v0;
        private Vertex _v1;
        private Vec2 _direction;
        private double _length;
        private List<Triangle> _faces;

        public double Length { get { return _length; } }
        public Vec2 Direction { get { return _direction; } }
        public Vertex V0 { get { return _v0; } }
        public Vertex V1 { get { return _v1; } }

        public Edge(Vertex v0, Vertex v1)
        {
            _v0 = v0;
            _v1 = v1;
            _faces = new List<Triangle>();
            _length = Vec2.Length(_v1.Position - _v0.Position);
            _direction = (_v1.Position - _v0.Position)/_length;
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
    public class Triangle
    {
        public Vertex V0;
        public Vertex V1;
        public Vertex V2;
        public Edge E0;
        public Edge E1;
        public Edge E2;
        public Triangle T0;
        public Triangle T1;
        public Triangle T2;

        public Triangle()
        {

        }
        public Triangle(Vec2 a, Vec2 b, Vec2 c)
        {
            var isCounterClockwise = Vec2.Cross(b - a, c - a) > 0;

            V0 = new Vertex(a);
            V1 = isCounterClockwise ? new Vertex(b) : new Vertex(c);
            V2 = isCounterClockwise ? new Vertex(c) : new Vertex(b);

            E0 = new Edge(V0, V1);
            E1 = new Edge(V1, V2);
            E2 = new Edge(V2, V0);

            V0.AddEdge(E0);
            V1.AddEdge(E0);
            V1.AddEdge(E1);
            V2.AddEdge(E1);
            V2.AddEdge(E2);
            V0.AddEdge(E2);
        }

        public Triangle(Vec2 a, Vec2 b)
        {
            V0 = new Vertex(a);
            V1 = new Vertex(b);

            E0 = new Edge(V0, V1);

            V0.AddEdge(E0);
            V1.AddEdge(E0);
        }

        public static bool CircumcircleContainsPoint(Triangle t, Vec2 p)
        {
            return CircumcircleContainsPoint(t.V0.Position, t.V1.Position, t.V2.Position, p);
        }

        public static bool CircumcircleContainsPoint(Vec2 a, Vec2 b, Vec2 c, Vec2 d)
        {
            if (Compare.Less(Vec2.Cross(b - a, c - a), 0))
            {
                var t = b;
                b = c;
                c = t;
            }

            double adx, ady, bdx, bdy, cdx, cdy, dx, dy, dnorm;

            dx = d.X;
            dy = d.Y;
            dnorm = Vec2.SquaredLength(d);
            adx = a.X - dx;
            ady = a.Y - dy;
            bdx = b.X - dx;
            bdy = b.Y - dy;
            cdx = c.X - dx;
            cdy = c.Y - dy;

            var det = (
                (Vec2.SquaredLength(a) - dnorm) * (bdx * cdy - bdy * cdx) +
                (Vec2.SquaredLength(b) - dnorm) * (cdx * ady - cdy * adx) +
                (Vec2.SquaredLength(c) - dnorm) * (adx * bdy - ady * bdx));

            return Compare.Greater(det, 0.0);
        }

        public override string ToString()
        {
            var v0 = V0 != null ? V0.ToString() : string.Empty;
            var v1 = V1 != null ? V1.ToString() : string.Empty;
            var v2 = V2 != null ? V2.ToString() : string.Empty;

            return "[" + v0 + "; " + v1 + "; " + v2 + "]";
        }

        public bool Contains(Edge edge)
        {
            return edge == E0 || edge == E1 || edge == E2;
        }
    }
}

