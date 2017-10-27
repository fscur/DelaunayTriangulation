using System;
using System.Collections.Generic;
using System.Linq;

namespace TriangleLib
{
    //TODO: parallelize!
    public class DelaunayTriangulation
    {
        //divide and conquer:
        //order points by X and Y coordinate
        //split point set until each division has 2 or 3 points
        //triangulate each division and merge
        public static List<Triangle> Triangulate(List<Vec2> points)
        {
            if (points.Count < 3)
                throw new ArgumentException("There must be at least three points to triangulate.");

            var orderedPoints = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            var triangles = Triangulate(orderedPoints, 0, points.Count - 1);
            return triangles.Where(t => t.V0 != null && t.V1 != null && t.V2 != null).ToList();
        }

        //recurse over points until it only find 2 or 3 points
        private static List<Triangle> Triangulate(List<Vec2> points, int startIndex, int endIndex)
        {
            var pointsCount = endIndex - startIndex + 1;

            if (pointsCount == 3)
            {
                return new List<Triangle>
                {
                    new Triangle(
                        new Vertex(points[startIndex + 0]),
                        new Vertex(points[startIndex + 1]),
                        new Vertex(points[startIndex + 2]))
                };
            }
            else if (pointsCount == 2)
            {
                return new List<Triangle>
                {
                    new Triangle(
                        new Vertex(points[startIndex + 0]),
                        new Vertex(points[startIndex + 1]))
                };
            }

            var midIndex = (startIndex + endIndex) / 2;
            var leftTriangles = Triangulate(points, startIndex, midIndex);
            var rightTriangles = Triangulate(points, midIndex + 1, endIndex);
            return Merge(leftTriangles, rightTriangles);
        }

        //create triangles merging both triangle sides
        private static List<Triangle> Merge(List<Triangle> leftTriangles, List<Triangle> rightTriangles)
        {
            var triangles = new List<Triangle>();
            var baseEdge = FindBottomMostEdge(leftTriangles, rightTriangles);

            while (true)
            {
                var leftCandidate = FindCandidateVertex(TrianglesDivideSide.Left, baseEdge, ref leftTriangles);
                var rightCandidate = FindCandidateVertex(TrianglesDivideSide.Right, baseEdge, ref rightTriangles);

                if (leftCandidate == null && rightCandidate == null)
                    break;

                var leftRightEdge = CreateLeftRightEdge(
                    baseEdge,
                    leftCandidate,
                    rightCandidate);

                var v0 = baseEdge.V0;
                var v1 = baseEdge.V1;
                var v2 = leftRightEdge.V0 == v0 || leftRightEdge.V0 == v1 ? leftRightEdge.V1 : leftRightEdge.V0;

                var triangle = new Triangle(v0, v1, v2);
                triangles.Add(triangle);
                
                baseEdge = leftRightEdge;
            }

            triangles.AddRange(leftTriangles);
            triangles.AddRange(rightTriangles);

            return triangles;
        }

        //find the bottom most edge connecting the left and right triangles that is part of the convex hull(N LOG N???)
        //NOTE: HOW TO IMPROVE THIS?
        //      the result of the merge could return each side`s most bottom hull vertex?
        private static Edge FindBottomMostEdge(List<Triangle> leftTriangles, List<Triangle> rightTriangles)
        {
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
                .ThenBy(p=> p.Position.Y)
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
                        else if (Compare.Equals(sin, 0.0) && Compare.Greater(testPosition.X, edge.V0.Position.X))
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
                        else if (Compare.Equals(sin, 0.0) && Compare.Less(testPosition.X, edge.V1.Position.X))
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

        private static bool IntersectEdges(List<Vertex> vertices, Edge edge)
        {
            foreach (var vertex in vertices)
            {
                foreach (var e in vertex.Edges)
                {
                    if (Edge.Intersects(e, edge))
                        return true;
                }
            }

            return false;
        }

        //find the edges connecting the left and right triangles (stitching)
        private static Edge CreateLeftRightEdge(
            Edge baseEdge,
            Vertex leftCandidate,
            Vertex rightCandidate)
        {
            Edge leftRightEdge = null;
            TrianglesDivideSide selectedSide = TrianglesDivideSide.None;

            //found 2 candidates? see which is inside which circumcircles...
            if (rightCandidate == null)
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

        //search for smallest angle edge from base edge that circumcircle does not have any point inside
        private static Vertex FindCandidateVertex(
            TrianglesDivideSide type,
            Edge baseEdge,
            ref List<Triangle> triangles)
        {
            Vertex baseVertex;
            Vec2 baseEdgeDirection;
            double angleSign;
            Vertex candidate = null;
            Edge candidateEdge;

            if (type == TrianglesDivideSide.Left)
            {
                angleSign = 1.0;
                baseVertex = baseEdge.V0;
                baseEdgeDirection = baseEdge.Direction;
            }
            else
            {
                angleSign = -1.0;
                baseVertex = baseEdge.V1;
                baseEdgeDirection = -baseEdge.Direction;
            }

            while (candidate == null)
            {
                Vertex nextPotentialCandidate = null;
                var minCandidateAngle = double.MaxValue;
                var edges = baseVertex.Edges;
                candidateEdge = null;

                for (int i = 0; i < edges.Count; i++)
                {
                    if (edges[i] == baseEdge)
                        continue;

                    var edge = edges[i];

                    var currentPotentialCandidate = edge.V0 == baseVertex ? edge.V1 : edge.V0;
                    Vec2 v0 = baseEdgeDirection;
                    Vec2 v1 = Vec2.Normalize(currentPotentialCandidate.Position - baseVertex.Position);
                    var currentAngle = -Vec2.Dot(v0, v1);

                    // if angle between base edge and candidate edge is the smallest angle and is less than 180 degrees
                    if (Compare.Greater(Vec2.Cross(v0, v1) * angleSign, 0) && 
                        Compare.Less(currentAngle, minCandidateAngle))
                    {
                        minCandidateAngle = currentAngle;

                        candidateEdge = edge;
                        candidate = currentPotentialCandidate;

                        int j = type == TrianglesDivideSide.Left ? 
                            (i + 1 < edges.Count ? i + 1 : 0) : 
                            (i == 0 ? edges.Count - 1 : i - 1);

                        nextPotentialCandidate = edges[j].V0 == baseVertex ? edges[j].V1 : edges[j].V0;
                    }
                }

                if (candidate == null)
                    break;

                //if found candidate, see if its circumcircle contains the next potential candidate
                //if it does not, then we are good to go
                //if it does, we have to remove its edge and start all over

                var a = candidate.Position;
                var b = baseEdge.V0.Position;
                var c = baseEdge.V1.Position;
                var p = nextPotentialCandidate.Position;

                var circumcircleContainsNextPotentialCandidate = Triangle.CircumcircleContainsPoint(a, b, c, p);

                if (circumcircleContainsNextPotentialCandidate)
                {
                    candidate = null;
                    baseVertex.RemoveEdge(candidateEdge);
                    triangles.RemoveAll(t => t.Contains(candidateEdge));

                    continue;
                }
            }

            return candidate;
        }
    }
}