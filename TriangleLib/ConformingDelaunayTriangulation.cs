using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    public class ConformingDelaunayTriangulation
    {
        //divide and conquer:
        //order points by X and Y coordinate
        //split point set until each division has 2 or 3 points
        //triangulate each division and merge
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

            if (vertices.Count < 2)
                return new List<Triangle>();

            var orderedVertices = vertices.OrderBy(p => p.Position.X).ThenBy(p => p.Position.Y).ToList();
            var triangles = Triangulate(pslg, orderedVertices, 0, vertices.Count - 1);
            return triangles.Where(t => t.V0 != null && t.V1 != null && t.V2 != null).ToList();
        }

        //recurse over points until it only find 2 or 3 points
        private static List<Triangle> Triangulate(PSLG pslg, List<Vertex> vertices, int startIndex, int endIndex)
        {
            var pointsCount = endIndex - startIndex + 1;

            if (pointsCount == 3)
            {
                return new List<Triangle>
                {
                    new Triangle(
                        vertices[startIndex + 0],
                        vertices[startIndex + 1],
                        vertices[startIndex + 2])
                };
            }
            else if (pointsCount == 2)
            {
                return new List<Triangle>
                {
                    new Triangle(
                        vertices[startIndex + 0],
                        vertices[startIndex + 1])
                };
            }

            var midIndex = (startIndex + endIndex) / 2;
            var leftTriangles = Triangulate(pslg, vertices, startIndex, midIndex);
            var rightTriangles = Triangulate(pslg, vertices, midIndex + 1, endIndex);
            return Merge(pslg, leftTriangles, rightTriangles);
        }

        //create triangles merging both triangle sides
        private static List<Triangle> Merge(PSLG pslg, List<Triangle> leftTriangles, List<Triangle> rightTriangles)
        {
            Edge leftRightEdge;
            Edge rightEdge;
            Edge leftEdge;

            var triangles = new List<Triangle>();
            var baseEdge = FindBottomMostEdge(pslg, leftTriangles, rightTriangles);

            var foundLeftRightEdge = FindLeftRightEdge(
                pslg,
                baseEdge,
                leftTriangles,
                rightTriangles,
                out leftRightEdge,
                out rightEdge,
                out leftEdge);

            if (!foundLeftRightEdge)
            {
                var triangle = new Triangle()
                {
                    V0 = baseEdge.V0,
                    V1 = baseEdge.V1,
                    E0 = baseEdge,
                };

                triangles.Add(triangle);
            }

            while (foundLeftRightEdge)
            {
                var v0 = baseEdge.V0;
                var v1 = baseEdge.V1;
                var v2 = leftRightEdge.V0 == v0 || leftRightEdge.V0 == v1 ? leftRightEdge.V1 : leftRightEdge.V0;
                
                var triangle = new Triangle(v0, v1, v2);

                triangles.Add(triangle);

                baseEdge = leftRightEdge;

                foundLeftRightEdge = FindLeftRightEdge(
                    pslg,
                    baseEdge,
                    leftTriangles,
                    rightTriangles,
                    out leftRightEdge,
                    out rightEdge,
                    out leftEdge);
            }

            triangles.AddRange(leftTriangles);
            triangles.AddRange(rightTriangles);

            return triangles;
        }

        //find the bottom most edge connecting the left and right triangles that is part of the convex hull(N LOG N???)
        //NOTE: HOW TO IMPROVE THIS?
        //      the result of the merge could return each side`s most bottom hull vertex?
        private static Edge FindBottomMostEdge(PSLG pslg, List<Triangle> leftTriangles, List<Triangle> rightTriangles)
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
        private static bool FindLeftRightEdge(
            PSLG pslg,
            Edge baseEdge,
            List<Triangle> leftTriangles,
            List<Triangle> rightTriangles,
            out Edge leftRightEdge,
            out Edge rightEdge,
            out Edge leftEdge)
        {
            var rightResult = FindCandidateVertex(pslg, TrianglesDivideSide.Right, baseEdge, ref rightTriangles);
            var leftResult = FindCandidateVertex(pslg, TrianglesDivideSide.Left, baseEdge, ref leftTriangles);

            rightEdge = rightResult.Edge;
            leftEdge = leftResult.Edge;
            leftRightEdge = null;

            if (!rightResult.Success && !leftResult.Success)
                return false;

            var foundRightEdgeCandidate = rightResult.Success;
            var foundLeftEdgeCandidate = leftResult.Success;

            TrianglesDivideSide selectedSide = TrianglesDivideSide.None;

            //found 2 candidates? see which is inside which circumcircles...
            if (!foundRightEdgeCandidate)
                selectedSide = TrianglesDivideSide.Left;
            else if (!foundLeftEdgeCandidate)
                selectedSide = TrianglesDivideSide.Right;
            else
            {
                var a = rightResult.Candidate.Position;
                var b = baseEdge.V0.Position;
                var c = baseEdge.V1.Position;
                var p = leftResult.Candidate.Position;

                selectedSide = !Triangle.CircumcircleContainsPoint(a, b, c, p) ? TrianglesDivideSide.Right : TrianglesDivideSide.Left;
            }

            switch (selectedSide)
            {
                case TrianglesDivideSide.Left:
                    leftRightEdge = new Edge(leftResult.Candidate, baseEdge.V1);

                    if (!leftResult.Candidate.Edges.Contains(leftRightEdge))
                        leftResult.Candidate.AddEdge(leftRightEdge);

                    if (!baseEdge.V1.Edges.Contains(leftRightEdge))
                        baseEdge.V1.AddEdge(leftRightEdge);
                    break;
                case TrianglesDivideSide.Right:
                    leftRightEdge = new Edge(baseEdge.V0, rightResult.Candidate);

                    if (!baseEdge.V0.Edges.Contains(leftRightEdge))
                        baseEdge.V0.AddEdge(leftRightEdge);

                    if (!rightResult.Candidate.Edges.Contains(leftRightEdge))
                        rightResult.Candidate.AddEdge(leftRightEdge);
                    break;
                default:
                    break;
            }

            return true;
        }

        //search for smallest angle edge from base edge that circumcircle does not have any point inside
        private static CandidateVertexResult FindCandidateVertex(
            PSLG pslg,
            TrianglesDivideSide type,
            Edge baseEdge,
            ref List<Triangle> triangles)
        {
            Vertex baseVertex;
            Vec2 baseEdgeDirection;
            double angleSign;

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

            CandidateVertexResult result = new CandidateVertexResult();

            while (result.Candidate == null)
            {
                Vertex nextPotentialCandidate = null;
                var minCandidateAngle = double.MaxValue;
                var edges = baseVertex.Edges;
                result.Edge = null;

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

                        result.Edge = edge;
                        result.Candidate = currentPotentialCandidate;

                        int j = type == TrianglesDivideSide.Left ?
                            (i + 1 < edges.Count ? i + 1 : 0) :
                            (i == 0 ? edges.Count - 1 : i - 1);

                        nextPotentialCandidate = edges[j].V0 == baseVertex ? edges[j].V1 : edges[j].V0;
                    }
                }

                //if found candidate, see if its circumcircle contains the next potential candidate
                //if it does not, then we are good to go
                //if it does, we have to remove its edge and start all over

                if (result.Candidate != null)
                {
                    var a = result.Candidate.Position;
                    var b = baseEdge.V0.Position;
                    var c = baseEdge.V1.Position;
                    var p = nextPotentialCandidate.Position;

                    var circumcircleContainsNextPotentialCandidate = Triangle.CircumcircleContainsPoint(a, b, c, p);

                    if (!circumcircleContainsNextPotentialCandidate)
                    {
                        result.Success = true;
                        return result;
                    }
                    else
                    {
                        if (pslg.Contains(result.Edge))
                        {
                            var edge = result.Edge;
                            var midVertex = new Vertex(edge.MidPoint);

                            triangles.RemoveAll(t => t.Contains(edge));

                            edge.V0.Edges.Remove(edge);
                            edge.V1.Edges.Remove(edge);

                            var e0 = new Edge(edge.V0, midVertex);
                            var e1 = new Edge(midVertex, edge.V1);

                            pslg.Edges.Remove(pslg.Find(edge.V0, edge.V1));
                            pslg.Edges.Add(e0);
                            pslg.Edges.Add(e1);

                            var t0 = new Triangle(midVertex, edge.V0, nextPotentialCandidate);
                            var t1 = new Triangle(midVertex, nextPotentialCandidate, edge.V1);

                            triangles.Add(t0);
                            triangles.Add(t1);

                            var flippingEdge0 = edge.V0.Find(nextPotentialCandidate);
                            var flippingEdge1 = edge.V1.Find(nextPotentialCandidate);

                            
                        }
                        else
                        {
                            baseVertex.RemoveEdge(result.Edge);
                            triangles.RemoveAll(t => t.Contains(result.Edge));
                        }

                        result.Candidate = null;

                        continue;
                    }
                }
                else
                {
                    result.Success = false;
                    return result;
                }
            }

            result.Success = false;
            return result;
        }
    }
}
