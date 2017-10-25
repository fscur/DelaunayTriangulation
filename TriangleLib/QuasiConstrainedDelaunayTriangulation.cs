﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    //This version does not delete any edges, so it can contain a lot of non-delaunay edges.
    public class QuasiConstrainedDelaunayTriangulation
    {
        enum EdgeType
        {
            Left,
            Right
        }

        struct CandidateVertexResult
        {
            internal Vertex Candidate;
            internal Edge Edge;
            internal bool Success;
        }

        //divide and conquer:
        //order points by X and Y coordinate
        //split point set until each division has 2 or 3 points
        //triangulate each division and merge
        public static List<Triangle> Triangulate(List<Vec2> points)
        {
            if (points.Count < 2)
                return new List<Triangle>();

            var orderedPoints = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            return Triangulate(orderedPoints, 0, points.Count - 1);
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
                        points[startIndex + 0],
                        points[startIndex + 1],
                        points[startIndex + 2])
                };
            }
            else if (pointsCount == 2)
            {
                return new List<Triangle>
                {
                    new Triangle(
                        points[startIndex + 0],
                        points[startIndex + 1])
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
            bool isFromRightTriangles;
            Edge leftRightEdge;
            Edge rightEdge;
            Edge leftEdge;

            var triangles = new List<Triangle>();
            var baseEdge = FindBottomMostEdge(leftTriangles, rightTriangles);

            var foundLeftRightEdge = FindLeftRightEdge(
                baseEdge,
                leftTriangles,
                rightTriangles,
                out leftRightEdge,
                out isFromRightTriangles,
                out rightEdge,
                out leftEdge);

            while (foundLeftRightEdge)
            {
                Vertex v0, v1, v2;
                Edge e0, e1, e2;

                if (isFromRightTriangles)
                {
                    v0 = leftRightEdge.V1;
                    v1 = baseEdge.V0;
                    v2 = baseEdge.V1;
                    e0 = leftRightEdge;
                    e1 = baseEdge;
                    e2 = rightEdge;

                    v0.AddEdge(leftRightEdge);
                    v1.AddEdge(leftRightEdge);
                }
                else
                {
                    v0 = leftRightEdge.V0;
                    v1 = baseEdge.V0;
                    v2 = baseEdge.V1;
                    e0 = leftEdge;
                    e1 = baseEdge;
                    e2 = leftRightEdge;

                    v0.AddEdge(leftRightEdge);
                    v2.AddEdge(leftRightEdge);
                }

                var triangle = new Triangle()
                {
                    V0 = v0,
                    V1 = v1,
                    V2 = v2,
                    E0 = e0,
                    E1 = e1,
                    E2 = e2
                };

                triangles.Add(triangle);

                baseEdge = leftRightEdge;
                foundLeftRightEdge = FindLeftRightEdge(
                    baseEdge,
                    leftTriangles,
                    rightTriangles,
                    out leftRightEdge,
                    out isFromRightTriangles,
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
            Edge baseEdge,
            List<Triangle> leftTriangles,
            List<Triangle> rightTriangles,
            out Edge leftRightEdge,
            out bool isFromRightTriangle,
            out Edge rightEdge,
            out Edge leftEdge)
        {
            var rightResult = FindCandidateVertex(EdgeType.Right, baseEdge, ref rightTriangles);
            var leftResult = FindCandidateVertex(EdgeType.Left, baseEdge, ref leftTriangles);

            leftRightEdge = null;
            isFromRightTriangle = false;
            var foundRightEdgeCandidate = rightResult.Success;
            var foundLeftEdgeCandidate = leftResult.Success;

            rightEdge = rightResult.Edge;
            leftEdge = leftResult.Edge;

            //found 2 candidates? see which is inside which circumcircles...
            if (foundRightEdgeCandidate && foundLeftEdgeCandidate)
            {
                var a = rightResult.Candidate.Position;
                var b = baseEdge.V0.Position;
                var c = baseEdge.V1.Position;
                var p = leftResult.Candidate.Position;

                if (!Triangle.CircumcircleContainsPoint(a, b, c, p))
                {
                    leftRightEdge = new Edge(baseEdge.V0, rightResult.Candidate);
                    isFromRightTriangle = true;
                }
                else
                {
                    leftRightEdge = new Edge(leftResult.Candidate, baseEdge.V1);
                    isFromRightTriangle = false;
                }

                return true;
            }
            else if (foundRightEdgeCandidate)
            {
                leftRightEdge = new Edge(baseEdge.V0, rightResult.Candidate);
                isFromRightTriangle = true;
                return true;
            }
            else if (foundLeftEdgeCandidate)
            {
                leftRightEdge = new Edge(leftResult.Candidate, baseEdge.V1);
                isFromRightTriangle = false;
                return true;
            }

            return false;
        }

        //search for smallest angle edge from base edge that circumcircle does not have any point inside
        private static CandidateVertexResult FindCandidateVertex(
            EdgeType type,
            Edge baseEdge,
            ref List<Triangle> triangles)
        {
            Vertex baseVertex;
            Vec2 baseEdgeDirection;
            double angleSign;

            if (type == EdgeType.Left)
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

                    int j = type == EdgeType.Left ?
                        (i + 1 < edges.Count ? i + 1 : 0) :
                        (i == 0 ? edges.Count - 1 : i - 1);

                    nextPotentialCandidate = edges[j].V0 == baseVertex ? edges[j].V1 : edges[j].V0;
                }
            }
            
            result.Success = result.Candidate != null;
            return result;
        }
    }
}