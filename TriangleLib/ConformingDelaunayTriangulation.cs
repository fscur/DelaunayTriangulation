using System;
using System.Collections.Generic;
using System.Linq;

namespace TriangleLib
{
    //TODO: parallelize!
    public class ConformingDelaunayTriangulation
    {
        private PSLG _pslg;

        private ConformingDelaunayTriangulation(PSLG pslg)
        {
            _pslg = pslg;
        }
        
        public static List<Triangle> Triangulate(PSLG pslg)
        {
            if (pslg == null)
                throw new ArgumentNullException();

            if (pslg.Edges.Count == 0)
                return new List<Triangle>();

            var triangulation = new ConformingDelaunayTriangulation(pslg);
            triangulation.PreProcess();
            return triangulation.Execute();
        }

        //before starting the triangulation, we have to find the pslg edges intersections 
        //and send them to the algorithm as vertices to be triangulated.
        //also, we have to split these intersecting edges in every intersecting vertex found
        private void PreProcess()
        {
            _pslg = CreatePSLGIntersections(_pslg);
        }

        private static PSLG CreatePSLGIntersections(PSLG pslg)
        {
            var pslgIntersections = FindPSLGIntersections(pslg);

            foreach (var edge in pslgIntersections.Keys)
            {
                //we must order edges by t or s param, so we can split the edges from v0 to v1
                var edgeIntersections = OrderPSLGEdgeIntersections(pslgIntersections, edge);
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

        private static List<Tuple<double, Edge.EdgeIntersection>> OrderPSLGEdgeIntersections(Dictionary<Edge, List<Edge.EdgeIntersection>> edgeIntersections, Edge edge)
        {
            var orderedIntersections = new List<Tuple<double, Edge.EdgeIntersection>>();

            foreach (var edgeIntersection in edgeIntersections[edge])
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
                        if (Compare.Greater(intersection.S, 0.0) &&
                            Compare.Less(intersection.S, 1.0) &&
                            Compare.Greater(intersection.T, 0.0) &&
                            Compare.Less(intersection.T, 1.0))
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
        
        //divide and conquer:
        //order points by X and Y coordinate
        //split point set until each division has 2 or 3 points
        //triangulate each division and merge

        private List<Triangle> Execute()
        {
            var vertices = new List<Vertex>();

            foreach (var edge in _pslg.Edges)
            {
                if (!vertices.Contains(edge.V0))
                    vertices.Add(edge.V0);

                if (!vertices.Contains(edge.V1))
                    vertices.Add(edge.V1);
            }

            if (vertices.Count < 3)
                throw new ArgumentException("There must be at least three vertices to triangulate.");

            var orderedVertices = vertices.OrderBy(p => p.Position.X).ThenBy(p => p.Position.Y).ToList();
            var triangles = DivideAndTriangulate(orderedVertices, 0, vertices.Count() - 1);
            return triangles.Where(t => !Triangle.IsDegenerate(t)).ToList();
        }

        private List<Triangle> DivideAndTriangulate(List<Vertex> vertices, int startIndex, int endIndex)
        {
            var pointsCount = endIndex - startIndex + 1;

            if (pointsCount == 3)
            {
                var triangle = new Triangle(
                    vertices[startIndex + 0],
                    vertices[startIndex + 1],
                    vertices[startIndex + 2]);

                //_pslg.RemoveEdge(triangle.E0);
                //_pslg.RemoveEdge(triangle.E1);
                //_pslg.RemoveEdge(triangle.E2);

                return new List<Triangle> { triangle };
            }
            else if (pointsCount == 2)
            {
                var triangle = new Triangle(
                        vertices[startIndex + 0],
                        vertices[startIndex + 1]);

                //_pslg.RemoveEdge(triangle.E0);

                return new List<Triangle> { triangle };
            }

            var midIndex = (startIndex + endIndex) / 2;
            var leftTriangles = DivideAndTriangulate(vertices, startIndex, midIndex);
            var rightTriangles = DivideAndTriangulate(vertices, midIndex + 1, endIndex);
            return Merge(leftTriangles, rightTriangles);
        }

        //create triangles merging both triangle sides
        private List<Triangle> Merge(List<Triangle> leftTriangles, List<Triangle> rightTriangles)
        {
            var triangles = new List<Triangle>();

            var baseEdge = FindBottomMostEdge(leftTriangles, rightTriangles);
            Vertex leftCandidate = null;
            Vertex rightCandidate = null;
            
            //when triangles are collinear, we have to add the base edge as a degenerate triangle
            leftCandidate = FindCandidateVertex(
                    TrianglesDivideSide.Left,
                    baseEdge,
                    ref leftTriangles,
                    rightTriangles);

            rightCandidate = FindCandidateVertex(
                    TrianglesDivideSide.Right,
                    baseEdge,
                    ref rightTriangles,
                    leftTriangles);

            if (leftCandidate == null && rightCandidate == null)
            {
                var t = new Triangle(baseEdge.V0, baseEdge.V1);
                triangles.Add(t);
            }

            while (leftCandidate != null || rightCandidate != null)
            {
                var leftRightEdge = CreateLeftRightEdge(
                    baseEdge,
                    leftCandidate,
                    rightCandidate);

                if (leftRightEdge == null && leftCandidate == rightCandidate)
                {
                    var t = new Triangle(baseEdge.V0, baseEdge.V1, leftCandidate);
                    triangles.Add(t);
                    break;
                }

                var v0 = baseEdge.V0;
                var v1 = baseEdge.V1;
                var v2 = leftRightEdge.V0 == v0 || leftRightEdge.V0 == v1 ? leftRightEdge.V1 : leftRightEdge.V0;

                var e0 = v0.Find(v1);
                var e1 = v1.Find(v2);
                var e2 = v2.Find(v0);

                e0.RemoveDegenerateTriangles();
                e1.RemoveDegenerateTriangles();
                e2.RemoveDegenerateTriangles();

                var triangle = new Triangle(v0, v1, v2);
                triangles.Add(triangle);

                //_pslg.RemoveEdge(triangle.E0);
                //_pslg.RemoveEdge(triangle.E1);
                //_pslg.RemoveEdge(triangle.E2);

                baseEdge = leftRightEdge;

                leftCandidate = FindCandidateVertex(
                    TrianglesDivideSide.Left,
                    baseEdge,
                    ref leftTriangles,
                    rightTriangles);

                rightCandidate = FindCandidateVertex(
                        TrianglesDivideSide.Right,
                        baseEdge,
                        ref rightTriangles,
                        leftTriangles);
            }

            triangles.AddRange(leftTriangles);
            triangles.AddRange(rightTriangles);

            return triangles;
        }

        //find the bottom most edge connecting the left and right triangles that is part of the convex hull(N LOG N???)
        //NOTE: HOW TO IMPROVE THIS?
        //      the result of the merge could return each side`s most bottom hull vertex?
        private Edge FindBottomMostEdge(List<Triangle> leftTriangles, List<Triangle> rightTriangles)
        {
            if (leftTriangles.Count == 0 || rightTriangles.Count == 0)
                throw new InvalidOperationException("There should be at least one triangle on each list.");

            var leftTriangleVertices = new List<Vertex>();
            foreach (var triangle in leftTriangles)
            {
                if (triangle.V0 != null && !leftTriangleVertices.Contains(triangle.V0))
                    leftTriangleVertices.Add(triangle.V0);
                if (triangle.V1 != null && !leftTriangleVertices.Contains(triangle.V1))
                    leftTriangleVertices.Add(triangle.V1);
                if (triangle.V2 != null && !leftTriangleVertices.Contains(triangle.V2))
                    leftTriangleVertices.Add(triangle.V2);
            }

            var rightTriangleVertices = new List<Vertex>();
            foreach (var triangle in rightTriangles)
            {
                if (triangle.V0 != null && !rightTriangleVertices.Contains(triangle.V0))
                    rightTriangleVertices.Add(triangle.V0);
                if (triangle.V1 != null && !rightTriangleVertices.Contains(triangle.V1))
                    rightTriangleVertices.Add(triangle.V1);
                if (triangle.V2 != null && !rightTriangleVertices.Contains(triangle.V2))
                    rightTriangleVertices.Add(triangle.V2);
            }

            // order left triangles from left to right, bottom to top
            leftTriangleVertices = leftTriangleVertices.OrderByDescending(p => p.Position.X)
                .ThenBy(p => p.Position.Y)
                .ToList();

            // order right triangles from right to left, bottom to top
            rightTriangleVertices = rightTriangleVertices.OrderBy(p => p.Position.X)
                .ThenBy(p => p.Position.Y)
                .ToList();

            for (int i = 0; i < leftTriangleVertices.Count; i++)
            {
                var leftVertex = leftTriangleVertices[i];

                for (int j = 0; j < rightTriangleVertices.Count; j++)
                {
                    var rightVertex = rightTriangleVertices[j];

                    var edge = new Edge(leftVertex, rightVertex);

                    bool notAllVerticesAreInsideHull = false;
                    bool verticesAreCollinearAndEdgeContainsVertex = false;

                    for (int k = 0; k < leftTriangleVertices.Count; k++)
                    {
                        if (k == i)
                            continue;

                        var testPosition = leftTriangleVertices[k].Position;

                        var v0 = edge.Direction;
                        var v1 = testPosition - edge.V0.Position;

                        var sin = Vec2.Cross(v0, v1);

                        if (Compare.Less(sin, 0.0))
                        {
                            notAllVerticesAreInsideHull = true;
                            break;
                        }
                        else if (Compare.Equals(sin, 0.0) && Compare.Greater(Vec2.Dot(v0, v1), 0.0) && Compare.Greater(edge.Length, Vec2.Length(v1)))
                        {
                            verticesAreCollinearAndEdgeContainsVertex = true;
                            break;
                        }
                    }

                    if (verticesAreCollinearAndEdgeContainsVertex || notAllVerticesAreInsideHull)
                        continue;

                    for (int k = 0; k < rightTriangleVertices.Count; k++)
                    {
                        if (k == j)
                            continue;

                        var testPosition = rightTriangleVertices[k].Position;

                        var v0 = edge.Direction;
                        var v1 = testPosition - edge.V0.Position;

                        var sin = Vec2.Cross(v0, v1);

                        if (Compare.Less(sin, 0.0))
                        {
                            notAllVerticesAreInsideHull = true;
                            break;
                        }
                        else if (Compare.Equals(sin, 0.0) && Compare.Greater(Vec2.Dot(v0, v1), 0.0) && Compare.Greater(edge.Length, Vec2.Length(v1)))
                        {
                            verticesAreCollinearAndEdgeContainsVertex = true;
                            break;
                        }
                    }

                    if (notAllVerticesAreInsideHull || verticesAreCollinearAndEdgeContainsVertex)
                        continue;

                    leftVertex.AddEdge(edge);
                    rightVertex.AddEdge(edge);

                    return edge;
                }
            }

            throw new InvalidOperationException("Could not find bottom most edge.");
        }

        //find the edges connecting the left and right triangles (stitching)
        private Edge CreateLeftRightEdge(
            Edge baseEdge,
            Vertex leftCandidate,
            Vertex rightCandidate)
        {
            Edge leftRightEdge = null;
            TrianglesDivideSide selectedSide = TrianglesDivideSide.None;

            //found 2 candidates? see which is inside which circumcircles...
            if (leftCandidate == rightCandidate)
            {
                var a = _pslg.Find(baseEdge.V0, leftCandidate);
                var b = _pslg.Find(baseEdge.V1, rightCandidate);

                if (a != null && b != null)
                {
                    if (baseEdge.V0.Find(leftCandidate) == null)
                        selectedSide = TrianglesDivideSide.Right;
                    else if (baseEdge.V1.Find(rightCandidate) == null)
                        selectedSide = TrianglesDivideSide.Left;
                }
                else if (a != null)
                    selectedSide = TrianglesDivideSide.Right;
                else if (b != null)
                    selectedSide = TrianglesDivideSide.Left;
            }
            else if (rightCandidate == null)
                selectedSide = TrianglesDivideSide.Left;
            else if (leftCandidate == null)
                selectedSide = TrianglesDivideSide.Right;
            else
            {
                var a = rightCandidate.Position;
                var b = baseEdge.V0.Position;
                var c = baseEdge.V1.Position;
                var p = leftCandidate.Position;

                selectedSide = !Triangle.CircumcircleContainsPoint(a, b, c, p) ? TrianglesDivideSide.Right : TrianglesDivideSide.Left;
            }

            switch (selectedSide)
            {
                case TrianglesDivideSide.Left:
                    leftRightEdge = new Edge(leftCandidate, baseEdge.V1);

                    if (!leftCandidate.Edges.Contains(leftRightEdge))
                        leftCandidate.AddEdge(leftRightEdge);

                    if (!baseEdge.V1.Edges.Contains(leftRightEdge))
                        baseEdge.V1.AddEdge(leftRightEdge);

                    break;
                case TrianglesDivideSide.Right:
                    leftRightEdge = new Edge(baseEdge.V0, rightCandidate);

                    if (!baseEdge.V0.Edges.Contains(leftRightEdge))
                        baseEdge.V0.AddEdge(leftRightEdge);

                    if (!rightCandidate.Edges.Contains(leftRightEdge))
                        rightCandidate.AddEdge(leftRightEdge);

                    break;
                default:
                    break;
            }

            return leftRightEdge;
        }

        internal struct FindCandidateParams
        {
            internal TrianglesDivideSide Side;
            internal Edge BaseEdge;
            internal Vertex BaseVertex;
            internal Vec2 BaseEdgeDirection;
            internal double AngleSign;
            internal List<Edge> Edges;
        }

        internal struct FindCandidateResult
        {
            internal Vertex Candidate;
            internal Edge CandidateEdge;
            internal Vertex NextPotentialCandidate;
        }

        //search for smallest angle edge from base edge in which circumcircle does not have any point inside
        private Vertex FindCandidateVertex(
            TrianglesDivideSide side,
            Edge baseEdge,
            ref List<Triangle> triangles,
            List<Triangle> oppositeTriangles)
        {
            Vertex candidate = null;

            while (candidate == null)
            {
                var findParams = BuildFindCandidateParams(side, baseEdge, oppositeTriangles.Concat(triangles).ToList());
                var findResult = FindCandidate(findParams);

                if (findResult.Candidate == null)
                    break;

                var candidateEdge = findResult.CandidateEdge;
                var candidateEdgeIntersection = EdgeIntersectsPSLG(candidateEdge);

                if (candidateEdgeIntersection.Intersects)
                {
                    var pslgEdge = candidateEdge == candidateEdgeIntersection.E0 ? candidateEdgeIntersection.E1 : candidateEdgeIntersection.E0;

                    DeleteEdge(candidateEdge, ref triangles);
                    var splitVertex = candidateEdgeIntersection.Vertex;
                    
                    var oppositeVertex = candidateEdge.Triangles.Count > 0 ? candidateEdge.FindOppositeVertex(candidateEdge.Triangles[0]) : null;

                    if (oppositeVertex != null)
                    {
                        RefineEdge(candidateEdge, oppositeVertex, splitVertex, ref triangles);
                    }
                    else
                    {
                        var v0 = candidateEdge.V0;
                        var v1 = candidateEdge.V1;

                        var e0 = new Edge(v0, splitVertex);
                        var e1 = new Edge(splitVertex, v1);

                        v0.AddEdge(e0);
                        splitVertex.AddEdge(e0);
                        splitVertex.AddEdge(e1);
                        v1.AddEdge(e1);
                    }

                    SplitPslgEdge(pslgEdge, splitVertex);
                    candidate = null;
                    continue;
                }

                candidate = findResult.Candidate;

                var edgeIsFromPslg = _pslg.Find(candidateEdge) != null;

                //if (edgeIsFromPslg)
                //    return candidate;

                var nextPotentialCandidate = findResult.NextPotentialCandidate;

                //if found candidate, see if its circumcircle contains the next potential candidate
                //if it does not, then we are good to go
                var a = candidate.Position;
                var b = baseEdge.V0.Position;
                var c = baseEdge.V1.Position;
                var p = nextPotentialCandidate.Position;

                if (!Triangle.CircumcircleContainsPoint(a, b, c, p))
                    return candidate;
                
                DeleteEdge(candidateEdge, ref triangles);

                //if candidate is from pslg, its possible that it does not belong to any triangle yet.
                //this will be true when its not yet connected to its next potential candidate
                //so, we must create two new triangles connecting it to the already merged set
                var candidateIsNotConnectedToMergedTriangles = nextPotentialCandidate.Find(candidate) == null;

                if (candidateIsNotConnectedToMergedTriangles)
                    ConnectCandidateToMergedTriangles(
                        candidate,
                        nextPotentialCandidate,
                        findParams.BaseVertex,
                        ref triangles);
                else if (edgeIsFromPslg)
                {
                    var midVertex = new Vertex(candidateEdge.MidPoint);
                    RefineEdge(candidateEdge, nextPotentialCandidate, midVertex, ref triangles);
                }

                candidate = null;
            }

            return candidate;
        }

        private Edge.EdgeIntersection EdgeIntersectsPSLG(Edge candidateEdge)
        {
            foreach (var e in _pslg.Edges)
            {
                var edgeIntersection = Edge.Intersect(e, candidateEdge);

                if (edgeIntersection.Intersects)
                {
                    if (Compare.Greater(edgeIntersection.S, 0.0) &&
                        Compare.Less(edgeIntersection.S, 1.0) &&
                        Compare.Greater(edgeIntersection.T, 0.0) &&
                        Compare.Less(edgeIntersection.T, 1.0))
                    {
                        return edgeIntersection;
                    }
                }
            }

            return new Edge.EdgeIntersection();
        }

        private void SplitPslgEdge(Edge edge, Vertex splitVertex)
        {
            var v0 = edge.V0;
            var v1 = edge.V1;

            var e0 = new Edge(v0, splitVertex);
            var e1 = new Edge(splitVertex, v1);

            _pslg.RemoveEdge(edge);
            edge.V1.RemoveEdge(edge);
            edge.V0.RemoveEdge(edge);

            _pslg.AddEdge(e0);
            _pslg.AddEdge(e1);

            //v0.AddEdge(e0);
            //splitVertex.AddEdge(e0);
            //splitVertex.AddEdge(e1);
            //v1.AddEdge(e1);
        }

        private void RefineEdge(Edge edge, Vertex oppositeVertex, Vertex splitVertex, ref List<Triangle> triangles)
        {
            var v0 = edge.V0;
            var v1 = edge.V1;
            var e0 = new Edge(v0, splitVertex);
            var e1 = new Edge(splitVertex, v1);

            //v0.AddEdge(e0);
            //splitVertex.AddEdge(e0);
            //splitVertex.AddEdge(e1);
            //v1.AddEdge(e1);

            if (_pslg.Contains(edge))
            {
                _pslg.RemoveEdge(edge);
                _pslg.Edges.Add(e0);
                _pslg.Edges.Add(e1);
            }

            var t0 = new Triangle(splitVertex, edge.V0, oppositeVertex);
            var t1 = new Triangle(splitVertex, oppositeVertex, edge.V1);

            triangles.Add(t0);
            triangles.Add(t1);

            //it must be a loop, because after we flip an edge, maybe we mess with the other edges.
            //is delaunay edge?
            //var flippingEdge0 = edge.V0.Find(oppositeVertex);
            //if (!Edge.IsDelaunay(flippingEdge0))
            //{
            //    var flippedEdge0 = flippingEdge0.FlipEdge();

            //    DeleteEdge(flippingEdge0, ref triangles);

            //    t0 = new Triangle(flippingEdge0.V0, flippedEdge0.V0, flippedEdge0.V1);
            //    t1 = new Triangle(flippingEdge0.V1, flippedEdge0.V1, flippedEdge0.V0);

            //    triangles.Add(t0);
            //    triangles.Add(t1);
            //}

            //var flippingEdge1 = edge.V1.Find(oppositeVertex);

            //if (!Edge.IsDelaunay(flippingEdge1))
            //{
            //    var flippedEdge1 = flippingEdge1.FlipEdge();

            //    DeleteEdge(flippingEdge1, ref triangles);

            //    t0 = new Triangle(flippingEdge1.V0, flippedEdge1.V1, flippedEdge1.V0);
            //    t1 = new Triangle(flippingEdge1.V1, flippedEdge1.V0, flippedEdge1.V1);

            //    triangles.Add(t0);
            //    triangles.Add(t1);
            //}
        }

        private void ConnectCandidateToMergedTriangles(Vertex candidate, Vertex nextPotentialCandidate, Vertex baseVertex, ref List<Triangle> triangles)
        {
            double max = double.MinValue;
            Vertex closestVertexFromCandidate = null;

            // find vertex of the next potential candidate that is closest to the candidate
            foreach (var e in nextPotentialCandidate.Edges)
            {
                if (e.V0 == baseVertex || e.V1 == baseVertex)
                    continue;

                var v0 = candidate.Position - nextPotentialCandidate.Position;
                var v1 = e.V0 == nextPotentialCandidate ? e.V1 : e.V0;

                var dot = Vec2.Dot(Vec2.Normalize(v0), Vec2.Normalize(v1.Position - nextPotentialCandidate.Position));
                if (dot > max)
                {
                    max = dot;
                    closestVertexFromCandidate = v1;
                }
            }

            var t0 = new Triangle(candidate, baseVertex, nextPotentialCandidate);
            var t1 = new Triangle(candidate, nextPotentialCandidate, closestVertexFromCandidate);

            triangles.Add(t0);
            triangles.Add(t1);
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

        private FindCandidateParams BuildFindCandidateParams(
            TrianglesDivideSide side,
            Edge baseEdge,
            List<Triangle> triangles)
        {
            var findParams = new FindCandidateParams();
            findParams.Side = side;
            findParams.BaseEdge = baseEdge;

            if (side == TrianglesDivideSide.Left)
            {
                findParams.AngleSign = 1.0;
                findParams.BaseVertex = baseEdge.V0;
                findParams.BaseEdgeDirection = baseEdge.Direction;
            }
            else
            {
                findParams.AngleSign = -1.0;
                findParams.BaseVertex = baseEdge.V1;
                findParams.BaseEdgeDirection = -baseEdge.Direction;
            }

            findParams.Edges = findParams.BaseVertex.Edges;

            //NOTE: try adding edges to the vertices when creating the pslg

            //select all the edges from the pslg that share the base vertex, except the base edge
            var pslgEdges = _pslg.Edges.Where(e =>
                (!(e.V0.Position == baseEdge.V0.Position && e.V1.Position == baseEdge.V1.Position) &&
                 !(e.V1.Position == baseEdge.V0.Position && e.V0.Position == baseEdge.V1.Position)) &&
                (e.V0.Position == findParams.BaseVertex.Position || e.V1.Position == findParams.BaseVertex.Position));

            //filter out all pslg edges also present in the base vertex edges
            pslgEdges = pslgEdges.Where(e0 => findParams.Edges.FirstOrDefault(e1 =>
                 (e0.V0.Position == e1.V0.Position && e0.V1.Position == e1.V1.Position) ||
                 (e0.V1.Position == e1.V0.Position && e0.V0.Position == e1.V1.Position)) == null);

            //filter out all pslg edges not present in the current triangle set
            pslgEdges = pslgEdges.Where(e => triangles.FirstOrDefault(
                t0 =>
                (t0.V0 != null && e.V0.Position == t0.V0.Position) ||
                (t0.V1 != null && e.V0.Position == t0.V1.Position) ||
                (t0.V2 != null && e.V0.Position == t0.V2.Position)) != null);

            pslgEdges = pslgEdges.Where(e => triangles.FirstOrDefault(
                t0 =>
                (t0.V0 != null && e.V1.Position == t0.V0.Position) ||
                (t0.V1 != null && e.V1.Position == t0.V1.Position) ||
                (t0.V2 != null && e.V1.Position == t0.V2.Position)) != null);

            //join edges
            findParams.Edges = findParams.Edges.Concat(pslgEdges).ToList();

            //edges must be ordered by angle so the algorithm can work
            //and as we have to insert the pslg edges into the list of edges for the current base vertex
            //we must sort them
            var edgeComparer = new EdgeAngleComparer(findParams.BaseVertex);
            findParams.Edges.Sort(edgeComparer);

            return findParams;
        }

        private FindCandidateResult FindCandidate(FindCandidateParams findParams)
        {
            var minCandidateAngle = double.MaxValue;
            var angleSign = findParams.AngleSign;
            var baseVertex = findParams.BaseVertex;
            var edges = findParams.Edges;
            var baseEdge = findParams.BaseEdge;
            var result = new FindCandidateResult();

            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i] == baseEdge)
                    continue;

                var edge = edges[i];

                var currentPotentialCandidate = edge.V0.Position == baseVertex.Position ? edge.V1 : edge.V0;
                Vec2 v0 = findParams.BaseEdgeDirection;
                Vec2 v1 = Vec2.Normalize(currentPotentialCandidate.Position - baseVertex.Position);
                var currentAngle = -Vec2.Dot(v0, v1);

                // if angle between base edge and candidate edge is the smallest angle and is less than 180 degrees
                if (Compare.Greater(Vec2.Cross(v0, v1) * angleSign, 0) &&
                    Compare.Less(currentAngle, minCandidateAngle))
                {
                    minCandidateAngle = currentAngle;

                    result.CandidateEdge = edge;
                    result.Candidate = currentPotentialCandidate;

                    int j = findParams.Side == TrianglesDivideSide.Left ?
                        (i + 1 < edges.Count ? i + 1 : 0) :
                        (i == 0 ? edges.Count - 1 : i - 1);

                    result.NextPotentialCandidate = edges[j].V0.Position == baseVertex.Position ? edges[j].V1 : edges[j].V0;
                }
            }

            return result;
        }
    }
}


