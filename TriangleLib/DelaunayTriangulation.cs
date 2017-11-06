using System;
using System.Collections.Generic;
using System.Linq;

namespace TriangleLib
{
    //TODO: parallelize!
    public class DelaunayTriangulation
    {
        private DelaunayTriangulationMode _mode;
        private PSLG _pslg;

        private DelaunayTriangulation(DelaunayTriangulationMode mode, PSLG pslg)
        {
            _mode = mode;
            _pslg = pslg;
        }

        private DelaunayTriangulation(DelaunayTriangulationMode mode = DelaunayTriangulationMode.Standard) : this(mode, null)
        {
        }

        public static List<Triangle> Triangulate(PSLG pslg)
        {
            if (pslg == null)
                throw new ArgumentNullException();

            if (pslg.Edges.Count == 0)
                return new List<Triangle>();

            var vertices = new List<Vertex>();

            foreach (var edge in pslg.Edges)
            {
                if (!vertices.Contains(edge.V0))
                    vertices.Add(edge.V0);

                if (!vertices.Contains(edge.V1))
                    vertices.Add(edge.V1);
            }

            if (vertices.Count < 3)
                throw new ArgumentException("There must be at least three vertices to triangulate.");

            var triangulation = new DelaunayTriangulation(DelaunayTriangulationMode.Conforming, pslg);
            return triangulation.ExecuteTriangulation(vertices);
        }
        
        public static List<Triangle> Triangulate(List<Vec2> points)
        {
            if (points.Count < 3)
                throw new ArgumentException("There must be at least three points to triangulate.");
            
            var triangulation = new DelaunayTriangulation(DelaunayTriangulationMode.Standard);
            var vertices = points.Select(p => new Vertex(p));
            return triangulation.ExecuteTriangulation(vertices);
        }

        //divide and conquer:
        //order points by X and Y coordinate
        //split point set until each division has 2 or 3 points
        //triangulate each division and merge

        private List<Triangle> ExecuteTriangulation(IEnumerable<Vertex> vertices)
        {
            var orderedVertices = vertices.OrderBy(p => p.Position.X).ThenBy(p => p.Position.Y).ToList();
            var triangles = DivideAndTriangulate(orderedVertices, 0, vertices.Count() - 1);
            return triangles;
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

                if (_mode == DelaunayTriangulationMode.Conforming)
                {
                    _pslg.RemoveEdge(triangle.E0);
                    _pslg.RemoveEdge(triangle.E1);
                    _pslg.RemoveEdge(triangle.E2);
                }

                return new List<Triangle> { triangle };
            }
            else if (pointsCount == 2)
            {
                var triangle = new Triangle(
                        vertices[startIndex + 0],
                        vertices[startIndex + 1]);

                if (_mode == DelaunayTriangulationMode.Conforming)
                    _pslg.RemoveEdge(triangle.E0);

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

            if (_mode == DelaunayTriangulationMode.Conforming)
                _pslg.RemoveEdge(baseEdge);

            while (true)
            {
                var leftCandidate = FindCandidateVertex(TrianglesDivideSide.Left, baseEdge, ref leftTriangles, rightTriangles);
                var rightCandidate = FindCandidateVertex(TrianglesDivideSide.Right, baseEdge, ref rightTriangles, leftTriangles);

                if (leftCandidate == null && rightCandidate == null)
                    break;

                var leftRightEdge = CreateLeftRightEdge(
                    baseEdge,
                    leftCandidate,
                    rightCandidate);

                if (leftRightEdge == null && leftCandidate == rightCandidate)
                {
                    triangles.Add(new Triangle(baseEdge.V0, baseEdge.V1, leftCandidate));

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
                
                if (_mode == DelaunayTriangulationMode.Conforming)
                {
                    _pslg.RemoveEdge(triangle.E0);
                    _pslg.RemoveEdge(triangle.E1);
                    _pslg.RemoveEdge(triangle.E2);
                }

                baseEdge = leftRightEdge;
            }

            triangles.AddRange(leftTriangles.Where(t => t.V0 != null && t.V1 != null && t.V2 != null));
            triangles.AddRange(rightTriangles.Where(t => t.V0 != null && t.V1 != null && t.V2 != null));

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
                if (baseEdge.V0.Find(leftCandidate) == null)
                    selectedSide = TrianglesDivideSide.Right;
                else if (baseEdge.V1.Find(rightCandidate) == null)
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

                candidate = findResult.Candidate;
                var nextPotentialCandidate = findResult.NextPotentialCandidate;

                //if found candidate, see if its circumcircle contains the next potential candidate
                //if it does not, then we are good to go
                var a = candidate.Position;
                var b = baseEdge.V0.Position;
                var c = baseEdge.V1.Position;
                var p = nextPotentialCandidate.Position;

                if (!Triangle.CircumcircleContainsPoint(a, b, c, p))
                    return candidate;

                var edge = findResult.CandidateEdge;

                DeleteEdge(edge, ref triangles);

                if (_mode == DelaunayTriangulationMode.Conforming)
                {
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
                    else
                        RefineEdge(edge, nextPotentialCandidate, ref triangles);
                }

                candidate = null;
            }

            return candidate;
        }

        private void RefineEdge(Edge edge, Vertex nextPotentialCandidate, ref List<Triangle> triangles)
        {
            var midVertex = new Vertex(edge.MidPoint);

            var e0 = new Edge(edge.V0, midVertex);
            var e1 = new Edge(midVertex, edge.V1);

            _pslg.RemoveEdge(edge);
            _pslg.Edges.Add(e0);
            _pslg.Edges.Add(e1);

            var t0 = new Triangle(midVertex, edge.V0, nextPotentialCandidate);
            var t1 = new Triangle(midVertex, nextPotentialCandidate, edge.V1);

            triangles.Add(t0);
            triangles.Add(t1);

            //it must be a loop, because after we flip an edge, maybe we mess with the other edges.
            //is delaunay edge?
            var flippingEdge0 = edge.V0.Find(nextPotentialCandidate);
            if (!Edge.IsDelaunay(flippingEdge0))
            {
                var flippedEdge0 = flippingEdge0.FlipEdge();

                DeleteEdge(flippingEdge0, ref triangles);

                t0 = new Triangle(flippingEdge0.V0, flippedEdge0.V0, flippedEdge0.V1);
                t1 = new Triangle(flippingEdge0.V1, flippedEdge0.V1, flippedEdge0.V0);

                triangles.Add(t0);
                triangles.Add(t1);
            }

            var flippingEdge1 = edge.V1.Find(nextPotentialCandidate);

            if (!Edge.IsDelaunay(flippingEdge1))
            {
                var flippedEdge1 = flippingEdge1.FlipEdge();

                DeleteEdge(flippingEdge1, ref triangles);

                t0 = new Triangle(flippingEdge1.V0, flippedEdge1.V1, flippedEdge1.V0);
                t1 = new Triangle(flippingEdge1.V1, flippedEdge1.V0, flippedEdge1.V1);

                triangles.Add(t0);
                triangles.Add(t1);
            }
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
            if (_mode == DelaunayTriangulationMode.Conforming)
            {
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
            }

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

