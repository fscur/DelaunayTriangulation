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
                triangulation.RemoveExtraEdges();

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
                points.RemoveAll(p => edge.V0.Position == p || edge.V1.Position == p);
            

            //extend "almost" touching end points to other edges
            _pslg = ExtendPSLGEdges(pslg);
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

        private PSLG ExtendPSLGEdges(PSLG pslg)
        {
            var newPslg = new PSLG();

            if (pslg.Edges.Count <= 1)
                return pslg;

            foreach (var edge in pslg.Edges)
            {
                var v0 = edge.V0;
                var v1 = edge.V1;

                foreach (var pslgEdge in pslg.Edges)
                {
                    if (pslgEdge == edge)
                        continue;

                    //check if point is close to the segment and is between v0 and v1
                    //TODO: check collinearity

                    var v = pslgEdge.V1.Position - pslgEdge.V0.Position;
                    var l = Vec2.Length(v);
                    var vn = Vec2.Normalize(v);
                    var c0 = Math.Abs(Vec2.Cross(vn, v0.Position - pslgEdge.V0.Position));
                    if (Compare.Greater(c0, 0.0) && Compare.Less(c0, _tolerance))
                    {
                        var d0 = Vec2.Dot(vn, v0.Position - pslgEdge.V0.Position);
                        if (Compare.Greater(d0, 0.0) && Compare.Less(d0, l))
                        {
                            v0 = new Vertex(pslgEdge.V0.Position + d0 * vn);
                        }
                    }

                    var c1 = Math.Abs(Vec2.Cross(vn, v1.Position - pslgEdge.V0.Position));
                    if (Compare.Greater(c1, 0.0) && Compare.Less(c1, _tolerance))
                    {
                        var d1 = Vec2.Dot(vn, v1.Position - pslgEdge.V0.Position);

                        if (Compare.Greater(d1, 0.0) && Compare.Less(d1, l))
                        {
                            v1 = new Vertex(pslgEdge.V0.Position + d1 * vn);
                        }
                    }
                }

                newPslg.AddEdge(new Edge(v0, v1));
            }

            return newPslg;
        }

        private PSLG CreatePSLGIntersections(PSLG pslg)
        {
            var pslgIntersections = FindPSLGIntersections(pslg);

            foreach (var edge in pslgIntersections.Keys)
            {
                //we must order edges by t or s param, so we can split the edges from v0 to v1
                var edgeIntersections = OrderPSLGEdgeIntersections(pslgIntersections[edge], edge);

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

                    var intersection = Edge.Intersect(e0, e1);

                    if (intersection.Intersects)
                    {
                        //var s0 = intersection.S * e1.Length;
                        //var t0 = intersection.T * e0.Length;

                        //if (Compare.Greater(s0, -_tolerance, Compare.TOLERANCE) &&
                        //    Compare.Less(s0, e1.Length + _tolerance, Compare.TOLERANCE) &&
                        //    Compare.Greater(t0, -_tolerance, Compare.TOLERANCE) &&
                        //    Compare.Less(t0, e0.Length + _tolerance, Compare.TOLERANCE))

                        if (Compare.Greater(intersection.S, 0.0, Compare.TOLERANCE) &&
                            Compare.Less(intersection.S, 1.0, Compare.TOLERANCE) &&
                            Compare.Greater(intersection.T, 0.0, Compare.TOLERANCE) &&
                            Compare.Less(intersection.T, 1.0, Compare.TOLERANCE))
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

        private static List<Tuple<double, Edge.EdgeIntersection>> OrderPSLGEdgeIntersections(List<Edge.EdgeIntersection> edgeIntersections, Edge edge)
        {
            var orderedIntersections = new List<Tuple<double, Edge.EdgeIntersection>>();

            foreach (var edgeIntersection in edgeIntersections)
            {
                if (edgeIntersection.E0 == edge)
                    orderedIntersections.Add(new Tuple<double, Edge.EdgeIntersection>(edgeIntersection.T, edgeIntersection));
                else
                    orderedIntersections.Add(new Tuple<double, Edge.EdgeIntersection>(edgeIntersection.S, edgeIntersection));
            }

            return orderedIntersections.OrderBy(i => i.Item1).ToList();
        }

        private static void SplitPSLGEdge(PSLG pslg, Edge edge, List<Tuple<double, Edge.EdgeIntersection>> edgeIntersections)
        {
            if (edgeIntersections.Count == 0)
                return;

            pslg.RemoveEdge(edge);

            Vertex v0 = edge.V0;
            Vertex v1 = edge.V1;
            Edge newEdge = null;

            for (int i = 0; i < edgeIntersections.Count; i++)
            {
                var intersectionVertex = edgeIntersections[i].Item2.Vertex;

                v0.RemoveEdge(edge);
                intersectionVertex.RemoveEdge(edge);

                newEdge = new Edge(v0, intersectionVertex);
                pslg.AddEdge(newEdge);

                v0 = intersectionVertex;
            }

            newEdge = new Edge(v0, v1);
            pslg.AddEdge(newEdge);
        }

        private void Execute()
        {
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
                    var edgeIntersection = Edge.Intersect(pslgEdge, edge);

                    if (Compare.Greater(edgeIntersection.S, 0.0, Compare.TOLERANCE) &&
                        Compare.Less(edgeIntersection.S, 1.0, Compare.TOLERANCE) &&
                        Compare.Greater(edgeIntersection.T, 0.0, Compare.TOLERANCE) &&
                        Compare.Less(edgeIntersection.T, 1.0, Compare.TOLERANCE) &&
                        (Vec2.Round(edgeIntersection.Vertex.Position) != edge.V0.Position &&
                        Vec2.Round(edgeIntersection.Vertex.Position) != edge.V1.Position))
                    {
                        intersections.Add(edgeIntersection);
                    }

                }

                if (intersections.Count == 0)
                    continue;

                var intersectingEdges = new Queue<Edge>();

                var orderedIntersections = OrderPSLGEdgeIntersections(intersections, pslgEdge).Select(i => i.Item2);

                foreach (var intersection in orderedIntersections)
                    intersectingEdges.Enqueue(intersection.E1);

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
                    else
                        intersectingEdges.Enqueue(edge);
                }
            }

            return _triangles;


            //while (triangleEdges.Count > 0)
            //{
            //    var edge = triangleEdges.Dequeue();

            //    if (Compare.Less(edge.Length, 1.0, Compare.TOLERANCE))
            //        continue;

            //    var intersections = EdgeIntersectsPSLG(edge);

            //    if (intersections.Count == 0)
            //        continue;

            //    DeleteEdge(edge, ref triangles);

            //    var orderedIntersections = OrderPSLGEdgeIntersections(intersections, edge);

            //    foreach (var triangle in edge.Triangles)
            //    {
            //        var oppositeVertex = edge.FindOppositeVertex(triangle);

            //        if (oppositeVertex == null)
            //            continue;

            //        var v0 = edge.V0;
            //        var v1 = edge.V1;
            //        var e0 = v0.Find(oppositeVertex);
            //        Edge e1 = null;
            //        Edge e2 = null;

            //        foreach (var intersectionTuple in orderedIntersections)
            //        {
            //            var intersection = intersectionTuple.Item2;
            //            var pslgEdge = edge == intersection.E0 ? intersection.E1 : intersection.E0;
            //            var splitVertex = intersection.Vertex;

            //            if (Compare.AlmostEqual(Vec2.Length(splitVertex.Position - oppositeVertex.Position), 0.0, Compare.TOLERANCE))
            //                continue;

            //            e1 = new Edge(v0, splitVertex);
            //            v0.AddEdge(e1);
            //            splitVertex.AddEdge(e1);

            //            e2 = new Edge(splitVertex, oppositeVertex);
            //            triangleEdges.Enqueue(e2);
            //            splitVertex.AddEdge(e2);
            //            oppositeVertex.AddEdge(e2);

            //            triangles.Add(new Triangle(e0, e1, e2));

            //            v0 = splitVertex;
            //            e0 = e2;
            //        }

            //        e1 = new Edge(v0, v1);
            //        v0.AddEdge(e1);
            //        v1.AddEdge(e1);

            //        e2 = v1.Find(oppositeVertex);

            //        triangles.Add(new Triangle(e0, e1, e2));
            //    }
            //}

            //return triangles.Where(t => !Triangle.IsDegenerate(t)).ToList();
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
            if (!_pslg.Contains(edge))
            {
                DeleteEdge(edge);

                var t = edge.Triangles[0];

                if (t.E0 != null && t.E0 != edge)
                    DeleteNonPslgEdges(t.E0);

                if (t.E1 != null && t.E1 != edge)
                    DeleteNonPslgEdges(t.E1);

                if (t.E2 != null && t.E2 != edge)
                    DeleteNonPslgEdges(t.E2);
            }
        }
    }
}