using System;
using System.Collections.Generic;
using System.Linq;

namespace TriangleLib
{
    public class ConformingDelaunayTriangulation
    {
        private DelaunayTriangulation _delaunayTriangulation;
        private PSLG _pslg;

        private ConformingDelaunayTriangulation(PSLG pslg)
        {
            _pslg = pslg;
            _delaunayTriangulation = new DelaunayTriangulation();
        }
        
        public static List<Triangle> Triangulate(PSLG pslg)
        {
            try
            {
                if (pslg == null)
                    throw new ArgumentNullException();

                if (pslg.Edges.Count == 0)
                    return new List<Triangle>();

                var triangulation = new ConformingDelaunayTriangulation(pslg);
                triangulation.PreProcess();
                var triangles = triangulation.Execute();
                triangles = triangulation.AddMissingPSLGEdges(triangles);
                return triangles;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to the ground.");
            }
        }
        
        private void PreProcess()
        {
            //before starting the triangulation, we have to find the pslg edges intersections 
            //and send them to the algorithm as vertices to be triangulated.
            //also, we have to split these intersecting edges in every intersecting vertex found
            _pslg = CreatePSLGIntersections(_pslg);

            //round point positions, so vertices close to each other are treated as the same
            _pslg = RoundPSLGVertices(_pslg);
        }

        private PSLG RoundPSLGVertices(PSLG pslg)
        {
            var rounded = new PSLG();

            var vertices = new Dictionary<Vec2, Vertex>();
            var digits = 6;

            for (int i = 0; i < pslg.Edges.Count; i++)
            {
                var edge = pslg.Edges[i];
                
                var p0 = Vec2.Round(edge.V0.Position, digits);
                if (!vertices.ContainsKey(p0))
                    vertices.Add(p0, new Vertex(p0));

                var p1 = Vec2.Round(edge.V1.Position, digits);
                if (!vertices.ContainsKey(p1))
                    vertices.Add(p1, new Vertex(p1));
            }

            for (int i = 0; i < pslg.Edges.Count; i++)
            {
                var edge = pslg.Edges[i];
                rounded.AddEdge(
                    new Edge(
                        vertices[Vec2.Round(edge.V0.Position, digits)], 
                        vertices[Vec2.Round(edge.V1.Position, digits)]));
            }

            return rounded;
        }

        private static PSLG CreatePSLGIntersections(PSLG pslg)
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

        private static void SplitPSLGEdge(PSLG pslg, Edge edge, List<Tuple<double, Edge.EdgeIntersection>> edgeIntersections)
        {
            Vertex v0 = edge.V0;
            Vertex v1 = null;
            Edge newEdge = null;
            v0.RemoveEdge(edge);

            pslg.RemoveEdge(edge);

            for (int i = 0; i < edgeIntersections.Count; i++)
            {
                var intersection = edgeIntersections[i].Item2;
                v1 = intersection.Vertex;

                newEdge = new Edge(v0, v1);
                pslg.AddEdge(newEdge);

                v0 = v1;
            }

            v1 = edge.V1;
            v1.RemoveEdge(edge);

            newEdge = new Edge(v0, v1);
            pslg.AddEdge(newEdge);
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

        private static Dictionary<Edge, List<Edge.EdgeIntersection>> FindPSLGIntersections(PSLG pslg)
        {
            var edgeIntersections = new Dictionary<Edge, List<Edge.EdgeIntersection>>();

            for (int i = 0; i < pslg.Edges.Count - 1; i++)
            {
                var e0 = pslg.Edges[i];

                for (int j = i + 1; j < pslg.Edges.Count; j++)
                {
                    var e1 = pslg.Edges[j];

                    var intersection = Edge.Intersect(e0, e1);

                    if (intersection.Intersects)
                    {
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
                }
            }

            return edgeIntersections;
        }
        
        private List<Triangle> Execute()
        {
            var points = new List<Vec2>();

            foreach (var edge in _pslg.Edges)
            {
                if (!points.Contains(edge.V0.Position))
                    points.Add(edge.V0.Position);

                if (!points.Contains(edge.V1.Position))
                    points.Add(edge.V1.Position);
            }

            if (points.Count < 3)
                throw new ArgumentException("There must be at least three vertices to triangulate.");

            return DelaunayTriangulation.Triangulate(points);
        }

        private List<Edge.EdgeIntersection> EdgeIntersectsPSLG(Edge edge)
        {
            var intersections = new List<Edge.EdgeIntersection>();

            foreach (var e in _pslg.Edges)
            {
                var edgeIntersection = Edge.Intersect(e, edge);

                if (edgeIntersection.Intersects)
                {
                    if (Compare.Greater(edgeIntersection.S, 0.5e-2, Compare.TOLERANCE) &&
                        Compare.Less(edgeIntersection.S, 1.0-0.5e-2, Compare.TOLERANCE) &&
                        Compare.Greater(edgeIntersection.T, 0.5e-2, Compare.TOLERANCE) &&
                        Compare.Less(edgeIntersection.T, 1.0-0.5e-2, Compare.TOLERANCE) &&
                        (Vec2.Round(edgeIntersection.Vertex.Position) != edge.V0.Position && 
                        Vec2.Round(edgeIntersection.Vertex.Position) != edge.V1.Position))
                    {
                        intersections.Add(edgeIntersection);
                    }
                }
            }

            return intersections;
        }
        
        private void DeleteEdge(Edge edge, ref List<Triangle> triangles)
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

            triangles.RemoveAll(t => t.Contains(edge));

            edge.V0.RemoveEdge(edge);
            edge.V1.RemoveEdge(edge);
        }

        private List<Triangle> AddMissingPSLGEdges(List<Triangle> triangles)
        {
            var triangleEdges = new Queue<Edge>();

            foreach (var triangle in triangles)
            {
                if (triangle.E0 != null && !triangleEdges.Contains(triangle.E0))
                    triangleEdges.Enqueue(triangle.E0);
                if (triangle.E1 != null && !triangleEdges.Contains(triangle.E1))
                    triangleEdges.Enqueue(triangle.E1);
                if (triangle.E2 != null && !triangleEdges.Contains(triangle.E2))
                    triangleEdges.Enqueue(triangle.E2);
            }

            while (triangleEdges.Count > 0)
            {
                var edge = triangleEdges.Dequeue();

                if (Compare.Less(edge.Length, 1.0, Compare.TOLERANCE))
                    continue;

                var intersections = EdgeIntersectsPSLG(edge);

                if (intersections.Count == 0)
                    continue;

                DeleteEdge(edge, ref triangles);

                var orderedIntersections = OrderPSLGEdgeIntersections(intersections, edge);

                foreach (var triangle in edge.Triangles)
                {
                    var oppositeVertex = edge.FindOppositeVertex(triangle);

                    if (oppositeVertex == null)
                        continue;

                    var v0 = edge.V0;
                    var v1 = edge.V1;
                    var e0 = v0.Find(oppositeVertex);
                    Edge e1 = null;
                    Edge e2 = null;

                    foreach (var intersectionTuple in orderedIntersections)
                    {
                        var intersection = intersectionTuple.Item2;
                        var pslgEdge = edge == intersection.E0 ? intersection.E1 : intersection.E0;
                        var splitVertex = intersection.Vertex;

                        if (Compare.AlmostEqual(Vec2.Length(splitVertex.Position - oppositeVertex.Position), 0.0, Compare.TOLERANCE))
                            continue;

                        e1 = new Edge(v0, splitVertex);
                        v0.AddEdge(e1);
                        splitVertex.AddEdge(e1);

                        e2 = new Edge(splitVertex, oppositeVertex);
                        triangleEdges.Enqueue(e2);
                        splitVertex.AddEdge(e2);
                        oppositeVertex.AddEdge(e2);

                        triangles.Add(new Triangle(e0, e1, e2));

                        v0 = splitVertex;
                        e0 = e2;
                    }

                    e1 = new Edge(v0, v1);
                    v0.AddEdge(e1);
                    v1.AddEdge(e1);

                    e2 = v1.Find(oppositeVertex);

                    triangles.Add(new Triangle(e0, e1, e2));
                }
            }

            return triangles.Where(t=>!Triangle.IsDegenerate(t)).ToList();
        }
    }
}


