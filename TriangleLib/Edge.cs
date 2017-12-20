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

        public bool CanFlip()
        {
            if (_triangles.Count < 2)
                return false;

            Vertex v0 = FindOppositeVertex(_triangles[0]);
            Vertex v1 = FindOppositeVertex(_triangles[1]);

            return Edge.Intersects(this, new Edge(v0, v1));
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
            Triangles.RemoveAll(t => Triangle.IsDegenerate(t));
        }

        public struct EdgeIntersection
        {
            public bool Intersects;
            public Edge E0;
            public Edge E1;
            public double S;
            public double T;
            public Vertex Vertex;
            public bool TrueIntersection;
        }
        
        public static List<EdgeIntersection> Intersect2(Edge e0, Edge e1, double tolerance)
        {
            var intersections = new List<EdgeIntersection>();

            //end points are intersecting?
            //have to test 2 intersections to account for edges with both endpoints inside the tolerance~!~~~~~!!!@!
            if (Compare.Less(Vec2.Length(e0.V0.Position - e1.V0.Position), 2.0 * tolerance))
            {
                intersections.Add(new EdgeIntersection()
                {
                    Intersects = true,
                    E0 = e0,
                    E1 = e1,
                    S = 0.0,
                    T = 0.0,
                    Vertex = new Vertex((e0.V0.Position + e1.V0.Position) * 0.5),
                    TrueIntersection = false
                });
            }

            if (Compare.Less(Vec2.Length(e0.V0.Position - e1.V1.Position), 2.0 * tolerance))
            {
                intersections.Add(new EdgeIntersection()
                {
                    Intersects = true,
                    E0 = e0,
                    E1 = e1,
                    S = 1.0,
                    T = 0.0,
                    Vertex = new Vertex((e0.V0.Position + e1.V1.Position) * 0.5),
                    TrueIntersection = false
                });
            }

            if (Compare.Less(Vec2.Length(e0.V1.Position - e1.V0.Position), 2.0 * tolerance))
            {
                intersections.Add(new EdgeIntersection()
                {
                    Intersects = true,
                    E0 = e0,
                    E1 = e1,
                    S = 0.0,
                    T = 1.0,
                    Vertex = new Vertex((e0.V1.Position + e1.V0.Position) * 0.5),
                    TrueIntersection = false
                });
            }

            if (Compare.Less(Vec2.Length(e0.V1.Position - e1.V1.Position), 2.0 * tolerance))
            {
                intersections.Add(new EdgeIntersection()
                {
                    Intersects = true,
                    E0 = e0,
                    E1 = e1,
                    S = 1.0,
                    T = 1.0,
                    Vertex = new Vertex((e0.V1.Position + e1.V1.Position) * 0.5),
                    TrueIntersection = false
                });
            }

            var edges = new Edge[] { e0, e0, e1, e1 };
            var points = new Vec2[] { e1.V0.Position, e1.V1.Position, e0.V0.Position, e0.V1.Position };
            var svalues = new double[] { 0.0, 1.0, 0.0, 1.0 };

            double s;
            double t;

            for (int i = 0; i < 4; i++)
            {
                var e = edges[i];
                var f = e == e0 ? e1 : e0;
                var closestPoint = ClosestPointToEdge(e, points[i], tolerance);
                t = closestPoint.T;

                s = ClosestPointToLine(f, closestPoint.Position, tolerance).T;

                if (Compare.Less(Math.Abs(closestPoint.SignedDistance), tolerance))
                {
                    intersections.Add(new EdgeIntersection()
                    {
                        Intersects = true,
                        E0 = e,
                        E1 = f,
                        S = s,
                        T = t,
                        Vertex = new Vertex(closestPoint.Position),
                        TrueIntersection = false
                    });
                }
            }

            if (intersections.Count > 1)
            {
                var vertices = intersections.Where(i => i.Intersects).Select(i => i.Vertex).ToList();
                intersections.Clear();

                var mergedVertices = VertexMerger.Merge(vertices, tolerance);
                var mergedIntersections = new List<EdgeIntersection>();

                foreach (var pair in mergedVertices)
                {
                    var v = pair.Key;
                    var averageT = ClosestPointToLine(e0, v.Position, tolerance).T;
                    var averageS = ClosestPointToLine(e1, v.Position, tolerance).T;

                    intersections.Add(new EdgeIntersection()
                    {
                        E0 = e0,
                        E1 = e1,
                        T = averageT,
                        S = averageS,
                        Intersects = true,
                        TrueIntersection = false,
                        Vertex = v
                    });
                }
            }

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
                return intersections;

            var x20 = x2 - x0;
            var y20 = y2 - y0;

            t = (x32 * y20 - x20 * y32) / det;
            s = (x10 * y20 - x20 * y10) / det;

            Vertex vertex = null;

            vertex = new Vertex(e0.V0.Position + t * (e0.V1.Position - e0.V0.Position));

            intersections.Add(new EdgeIntersection()
            {
                Intersects = true,
                E0 = e0,
                E1 = e1,
                S = Math.Round(s, 6),
                T = Math.Round(t, 6),
                Vertex = vertex,
                TrueIntersection = true
            });

            return intersections;
        }

        public static double Distance(Edge e, Vec2 p, double r)
        {
            var a = e.V0.Position;
            var b = e.V1.Position;
            var ap = p - a;
            var ab = b - a;
            var t = Vec2.Clamp(Vec2.Dot(ap, ab) / Vec2.Dot(ab, ab), 0.0, 1.0);
            return Math.Abs(Vec2.Length(ap - ab * t) - r);
        }

        public struct EdgeClosestPoint
        {
            public Vec2 Position;
            public double T;
            public double SignedDistance;
        }

        public static EdgeClosestPoint ClosestPointToEdge(Edge e, Vec2 p, double tolerance)
        {
            var a = e.V0.Position;
            var b = e.V1.Position;
            var c = p;

            var ab = b - a;

            double t = Vec2.Dot(c - a, ab) / Vec2.Dot(ab, ab);
            
            if (t < 0.0f)
                t = 0.0f;

            if (t > 1.0f)
                t = 1.0f;
            
            var point = a + t * ab;
            //TODO: optimize using squared distance comparisons
            var signedDistance = Vec2.Length((p - a) - ab * t) - tolerance;
            return new EdgeClosestPoint()
            {
                Position = point,
                T = t,
                SignedDistance = signedDistance
            };
        }

        public static EdgeClosestPoint ClosestPointToLine(Edge e, Vec2 p, double tolerance)
        {
            var a = e.V0.Position;
            var b = e.V1.Position;
            var c = p;

            var ab = b - a;

            double t = Vec2.Dot(c - a, ab) / Vec2.Dot(ab, ab);
            
            var point = a + t * ab;
            //TODO: optimize using squared distance comparisons
            var signedDistance = Vec2.Length((p - a) - ab * t) - tolerance;
            return new EdgeClosestPoint()
            {
                Position = point,
                T = t,
                SignedDistance = signedDistance
            };
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

            if (Compare.AlmostEqual(det, 0.0, 0.5E-10))
                return new EdgeIntersection() { Intersects = false };

            var x20 = x2 - x0;
            var y20 = y2 - y0;

            var t = (x32 * y20 - x20 * y32) / det;
            var s = (x10 * y20 - x20 * y10) / det;

            Vertex vertex = null;

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

        public static Edge Extend(Edge edge, Vertex edgeVertex, Vertex vertex)
        {
            if (edge.V0 == edgeVertex)
                return new Edge(vertex, edge.V1);
            else if (edge.V1 == edgeVertex)
                return new Edge(edge.V0, vertex);

            throw new ArgumentException("Edge must contain edgeVertex.");
        }
    }
}
