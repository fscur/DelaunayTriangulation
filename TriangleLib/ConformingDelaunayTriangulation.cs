using System;
using System.Collections.Generic;
using System.Linq;

namespace TriangleLib
{
    public class ConformingDelaunayTriangulation
    {
        public ConformingDelaunayTriangulation(PSLG pslg, double tolerance)
        {
            _tolerance = tolerance;
            _pslg = pslg;
        }

        private double _tolerance;

        private DelaunayTriangulation _delaunayTriangulation;

        private List<Triangle> _triangles = new List<Triangle>();
        public List<Triangle> Triangles
        {
            get { return _triangles; }
        }

        private PSLG _pslg;
        public PSLG Pslg
        {
            get { return _pslg; }
        }

        public static ConformingDelaunayTriangulation Triangulate(PSLG pslg, List<Vertex> vertices, double tolerance)
        {
            var points = vertices.Select(v => v.Position).ToList();
            return Triangulate(pslg, points, tolerance);
        }

        public static ConformingDelaunayTriangulation Triangulate(PSLG pslg, List<Vec2> points, double tolerance)
        {
            try
            {
                var triangulation = new ConformingDelaunayTriangulation(pslg, tolerance);

                if (pslg.Vertices.Count + points.Count < 3)
                    return triangulation;

                triangulation.PreProcess(pslg, points);
                triangulation.Execute();
                triangulation.AddMissingPSLGEdges();
                //triangulation.RemoveExtraEdges();

                return triangulation;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to the ground.");
            }
        }

        private void PreProcess(PSLG pslg, List<Vec2> points)
        {
            _pslg = pslg;

            foreach (var edge in _pslg.Edges)
            {
                edge.V0.RemoveEdge(edge);
                edge.V1.RemoveEdge(edge);
                points.RemoveAll(p => edge.V0.Position == p || edge.V1.Position == p);
            }
            
            //before starting the triangulation, we have to find the pslg edges intersections 
            //and send them to the algorithm as vertices to be triangulated.
            //also, we have to split these intersecting edges in every intersecting vertex found
            _pslg = CreatePSLGIntersections(_pslg);
            //merge vertex positions, so vertices close to each other are treated as the same
            _pslg = MergePSLGVertices(_pslg, points);
        }

        private PSLG MergePSLGVertices(PSLG pslg, List<Vec2> points)
        {
            var verticesToMerge = new List<Vertex>();
            verticesToMerge.AddRange(points.Select(p=>new Vertex(p)));

            for (int i = 0; i < _pslg.Edges.Count; i++)
            {
                var edge = _pslg.Edges[i];

                verticesToMerge.Add(edge.V0);
                verticesToMerge.Add(edge.V1);
            }

            var mergedVertices = VertexMerger.Merge(verticesToMerge, _tolerance);

            Func<Vertex, Vertex> findMergedVertex = (Vertex v) =>
            {
                foreach (var pair in mergedVertices)
                {
                    var mergedPoint = pair.Key;
                    var closePoints = pair.Value;

                    if (closePoints.Contains(v))
                        return mergedPoint;
                }

                return null;
            };

            var rounded = new PSLG();

            for (int i = 0; i < _pslg.Edges.Count; i++)
            {
                var edge = _pslg.Edges[i];

                var v0 = findMergedVertex(edge.V0);
                var v1 = findMergedVertex(edge.V1);

                rounded.AddEdge(new Edge(v0, v1));
            }

            foreach (var vertex in mergedVertices.Keys)
                rounded.AddVertex(vertex);

            return rounded;
        }

        private PSLG CreatePSLGIntersections(PSLG pslg)
        {
            var pslgIntersections = FindPSLGIntersections(pslg);

            foreach (var edge in pslgIntersections.Keys)
            {
                //we must order edges by t or s param, so we can split the edges from v0 to v1
                var edgeIntersections = OrderPSLGEdgeIntersections(pslgIntersections[edge], edge, true);

                SplitPSLGEdge(pslg, edge, edgeIntersections);
            }

            return pslg;
        }

        private Dictionary<Edge, List<Edge.EdgeIntersection>> FindPSLGIntersections(PSLG pslg)
        {
            var edgeIntersections = new Dictionary<Edge, List<Edge.EdgeIntersection>>();
            var newPslg = new PSLG();
            
            for (int i = 0; i < pslg.Edges.Count - 1; i++)
            {
                var e0 = pslg.Edges[i];

                for (int j = i + 1; j < pslg.Edges.Count; j++)
                {
                    var e1 = pslg.Edges[j];

                    var intersection = Edge.Intersect2(e0, e1, _tolerance);

                    if (intersection.Intersects)
                    {
                        var s0 = intersection.S * e1.Length;
                        var t0 = intersection.T * e0.Length;

                        var gap = 2.0 * _tolerance;

                        if (!intersection.TrueIntersection || 
                            Compare.Greater(s0, -gap, Compare.TOLERANCE) &&
                            Compare.Less(s0, e1.Length + gap, Compare.TOLERANCE) &&
                            Compare.Greater(t0, -gap, Compare.TOLERANCE) &&
                            Compare.Less(t0, e0.Length + gap, Compare.TOLERANCE))
                        {
                            if (!edgeIntersections.ContainsKey(e0))
                                edgeIntersections.Add(e0, new List<Edge.EdgeIntersection>() { intersection });
                            else
                                edgeIntersections[e0].Add(intersection);

                            if (!edgeIntersections.ContainsKey(e1))
                                edgeIntersections.Add(e1, new List<Edge.EdgeIntersection>() { intersection });
                            else
                                edgeIntersections[e1].Add(intersection);
                        }
                    }
                    else
                    {
                        var v0 = e0.V0;
                        var v1 = e0.V1;
                        var v = e1.V1.Position - e1.V0.Position;
                        var l = Vec2.Length(v);
                        var vn = Vec2.Normalize(v);
                        var d0 = Vec2.Dot(vn, v0.Position - e1.V0.Position);
                        if (Compare.Greater(d0, 0.0) && Compare.Less(d0, l))
                        {
                            intersection = new Edge.EdgeIntersection()
                            {
                                E0 = e0,
                                E1 = e1,
                                S = d0 / l,
                                T = 0.0,
                                Vertex = v0
                            };

                            if (!edgeIntersections.ContainsKey(e1))
                                edgeIntersections.Add(e1, new List<Edge.EdgeIntersection>() { intersection });
                            else
                                edgeIntersections[e1].Add(intersection);
                        }

                        var d1 = Vec2.Dot(vn, v1.Position - e1.V0.Position);
                        if (Compare.Greater(d1, 0.0) && Compare.Less(d1, l))
                        {
                            intersection = new Edge.EdgeIntersection()
                            {
                                E0 = e0,
                                E1 = e1,
                                S = d1 / l,
                                T = 1.0,
                                Vertex = v1
                            };

                            if (!edgeIntersections.ContainsKey(e1))
                                edgeIntersections.Add(e1, new List<Edge.EdgeIntersection>() { intersection });
                            else
                                edgeIntersections[e1].Add(intersection);
                        }

                        //second edge
                        v0 = e1.V0;
                        v1 = e1.V1;
                        v = e0.V1.Position - e0.V0.Position;
                        l = Vec2.Length(v);
                        vn = Vec2.Normalize(v);
                        d0 = Vec2.Dot(vn, v0.Position - e0.V0.Position);

                        if (Compare.Greater(d0, 0.0) && Compare.Less(d0, l))
                        {
                            intersection = new Edge.EdgeIntersection()
                            {
                                E0=e1,
                                E1=e0,
                                S = d0/l,
                                T = 0.0,
                                Vertex = v0
                            };

                            if (!edgeIntersections.ContainsKey(e0))
                                edgeIntersections.Add(e0, new List<Edge.EdgeIntersection>() { intersection });
                            else
                                edgeIntersections[e0].Add(intersection);
                        }

                        d1 = Vec2.Dot(vn, v1.Position - e0.V0.Position);
                        if (Compare.Greater(d1, 0.0) && Compare.Less(d1, l))
                        {
                            intersection = new Edge.EdgeIntersection()
                            {
                                E0 = e1,
                                E1 = e0,
                                S = d1 / l,
                                T = 1.0,
                                Vertex = v1
                            };

                            if (!edgeIntersections.ContainsKey(e0))
                                edgeIntersections.Add(e0, new List<Edge.EdgeIntersection>() { intersection });
                            else
                                edgeIntersections[e0].Add(intersection);
                        }
                    }
                }
            }

            return edgeIntersections;
        }

        private static List<Tuple<double, Edge.EdgeIntersection>> OrderPSLGEdgeIntersections(
            List<Edge.EdgeIntersection> edgeIntersections, 
            Edge edge,
            bool insertEndPointsAsIntersections)
        {
            var orderedIntersections = new List<Tuple<double, Edge.EdgeIntersection>>();

            foreach (var edgeIntersection in edgeIntersections)
            {
                if (edgeIntersection.E0 == edge)
                    orderedIntersections.Add(new Tuple<double, Edge.EdgeIntersection>(edgeIntersection.T, edgeIntersection));
                else
                    orderedIntersections.Add(new Tuple<double, Edge.EdgeIntersection>(edgeIntersection.S, edgeIntersection));
            }

            if (insertEndPointsAsIntersections)
            {
                // insert endpoints as fake intersections, so we can use them in the split algorithm
                orderedIntersections.Add(new Tuple<double, Edge.EdgeIntersection>(0.0, new Edge.EdgeIntersection()
                {
                    E0 = edge,
                    E1 = null,
                    Intersects = true,
                    S = 0.0,
                    T = 0.0,
                    TrueIntersection = true,
                    Vertex = edge.V0
                }));

                orderedIntersections.Add(new Tuple<double, Edge.EdgeIntersection>(1.0, new Edge.EdgeIntersection()
                {
                    E0 = edge,
                    E1 = null,
                    Intersects = true,
                    S = 1.0,
                    T = 1.0,
                    TrueIntersection = true,
                    Vertex = edge.V1
                }));
            }

            return orderedIntersections.OrderBy(i => i.Item1).ToList();
        }

        private static void SplitPSLGEdge(PSLG pslg, Edge edge, List<Tuple<double, Edge.EdgeIntersection>> edgeIntersections)
        {
            if (edgeIntersections.Count == 0)
                return;

            pslg.RemoveEdge(edge);

            
            Edge newEdge = null;

            for (int i = 0; i < edgeIntersections.Count-1; i++)
            {
                Vertex v0 = edgeIntersections[i+0].Item2.Vertex;
                Vertex v1 = edgeIntersections[i+1].Item2.Vertex;

                v0.RemoveEdge(edge);
                v1.RemoveEdge(edge);

                newEdge = new Edge(v0, v1);
                pslg.AddEdge(newEdge);
            }
        }

        private void Execute()
        {
            if (_pslg.Vertices.Count < 3)
                return;

            _delaunayTriangulation = DelaunayTriangulation.Triangulate(_pslg.Vertices);
            _triangles = _delaunayTriangulation.Triangles;
        }

        private List<Edge.EdgeIntersection> EdgeIntersectsPSLG(Edge edge)
        {
            var intersections = new List<Edge.EdgeIntersection>();

            foreach (var pslgEdge in _pslg.Edges)
            {
                var edgeIntersection = Edge.Intersect(pslgEdge, edge);

                if (edgeIntersection.Intersects)
                {
                    if (Compare.Greater(edgeIntersection.S, 0.5e-2, Compare.TOLERANCE) &&
                        Compare.Less(edgeIntersection.S, 1.0 - 0.5e-2, Compare.TOLERANCE) &&
                        Compare.Greater(edgeIntersection.T, 0.5e-2, Compare.TOLERANCE) &&
                        Compare.Less(edgeIntersection.T, 1.0 - 0.5e-2, Compare.TOLERANCE) &&
                        (Vec2.Round(edgeIntersection.Vertex.Position) != edge.V0.Position &&
                        Vec2.Round(edgeIntersection.Vertex.Position) != edge.V1.Position))
                    {
                        intersections.Add(edgeIntersection);
                    }
                }
            }

            return intersections;
        }

        private void DeleteEdge(Edge edge)
        {
            var trianglesToRemove = edge.Triangles;

            foreach (var triangle in trianglesToRemove)
            {
                if (triangle.E0 != null && triangle.E0 != edge)
                    triangle.E0.Triangles.Remove(triangle);
                if (triangle.E1 != null && triangle.E1 != edge)
                    triangle.E1.Triangles.Remove(triangle);
                if (triangle.E2 != null && triangle.E2 != edge)
                    triangle.E2.Triangles.Remove(triangle);
            }

            _triangles.RemoveAll(t => t.Contains(edge));

            edge.V0.RemoveEdge(edge);
            edge.V1.RemoveEdge(edge);
        }

        //insert pslg edges not present in the triangulation
        private List<Triangle> AddMissingPSLGEdges()
        {
            foreach (var pslgEdge in _pslg.Edges)
            {
                var triangleEdges = new List<Edge>();

                foreach (var triangle in _triangles)
                {
                    if (triangle.E0 != null && !triangleEdges.Contains(triangle.E0))
                        triangleEdges.Add(triangle.E0);
                    if (triangle.E1 != null && !triangleEdges.Contains(triangle.E1))
                        triangleEdges.Add(triangle.E1);
                    if (triangle.E2 != null && !triangleEdges.Contains(triangle.E2))
                        triangleEdges.Add(triangle.E2);
                }

                var intersections = new List<Edge.EdgeIntersection>();
                foreach (var edge in triangleEdges)
                {
                    var intersection = Edge.Intersect2(pslgEdge, edge, _tolerance);

                    var t = intersection.T;
                    var s = intersection.S;
                    
                    if (Compare.Greater(s, 0.0, Compare.TOLERANCE) &&
                        Compare.Less(s, 1.0, Compare.TOLERANCE) &&
                        Compare.Greater(t, 0.0, Compare.TOLERANCE) &&
                        Compare.Less(t, 1.0, Compare.TOLERANCE))
                    {
                        intersections.Add(intersection);
                    }
                }

                if (intersections.Count == 0)
                    continue;

                var intersectingEdges = new Queue<Edge>();

                var orderedIntersections = OrderPSLGEdgeIntersections(intersections, pslgEdge, false).Select(i => i.Item2);

                foreach (var intersection in orderedIntersections)
                    intersectingEdges.Enqueue(intersection.E1);

                Edge lastIntersectingEdge = null;

                while (intersectingEdges.Count > 0)
                {
                    var edge = intersectingEdges.Dequeue();
                    if (edge.CanFlip())
                    {
                        var flipEdge = edge.FlipEdge();

                        if (Edge.Intersects(pslgEdge, flipEdge))
                        {
                            intersectingEdges.Enqueue(edge);
                            continue;
                        }

                        DeleteEdge(edge);

                        _triangles.Add(new Triangle(edge.V0, flipEdge.V0, flipEdge.V1));
                        _triangles.Add(new Triangle(flipEdge.V0, flipEdge.V1, edge.V1));
                    }
                    else if (lastIntersectingEdge != null && edge != lastIntersectingEdge)
                        intersectingEdges.Enqueue(edge);

                    lastIntersectingEdge = edge;
                }
            }

            return _triangles;
        }

        //find the convex hull and, start from first edge of hull and 
        //recursively delete edges that are not pslg
        private List<Triangle> RemoveExtraEdges()
        {
            var vertices = new List<Vertex>();

            Action<Vertex> addVertex = (vertex) =>
            {
                if (vertex != null && !vertices.Contains(vertex))
                    vertices.Add(vertex);
            };

            foreach (var triangle in _triangles)
            {
                addVertex(triangle.V0);
                addVertex(triangle.V1);
                addVertex(triangle.V2);
            }

            var hull = ConvexHull.GrahamScan(vertices);

            DeleteNonPslgEdges(hull);

            return _triangles;
        }

        private void DeleteNonPslgEdges(List<Vertex> hull)
        {
            var v0 = hull[0];
            hull.RemoveAt(0);

            var v1 = hull[0];
            hull.RemoveAt(0);

            while (hull.Count > 0)
            {
                var edge = v0.Find(v1);

                if (edge != null)
                    DeleteNonPslgEdges(edge);

                v0 = v1;
                v1 = hull[0];
                hull.RemoveAt(0);
            }
        }

        private void DeleteNonPslgEdges(Edge edge)
        {
            try
            {

                if (!_pslg.Contains(edge))
                {
                    DeleteEdge(edge);

                    if (edge.Triangles.Count == 0)
                        return;

                    var t = edge.Triangles[0];

                    if (t.E0 != null && t.E0 != edge)
                        DeleteNonPslgEdges(t.E0);

                    if (t.E1 != null && t.E1 != edge)
                        DeleteNonPslgEdges(t.E1);

                    if (t.E2 != null && t.E2 != edge)
                        DeleteNonPslgEdges(t.E2);
                }
            }
            catch (Exception ex)
            {
                if (true)
                { }
            }
        }
    }
}