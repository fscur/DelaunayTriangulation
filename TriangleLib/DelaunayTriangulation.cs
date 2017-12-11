using System;
using System.Collections.Generic;
using System.Linq;

namespace TriangleLib
{
    //TODO: parallelize!
    //https://www.youtube.com/watch?v=FUkmgjB3tSg
    public class DelaunayTriangulation
    {
        private List<Edge> _edges;
        public List<Edge> Edges
        {
            get
            {
                return _edges;
            }
        }

        private List<Triangle> _triangles;
        public List<Triangle> Triangles
        {
            get
            {
                return _triangles;
            }
        }

        private DelaunayTriangulation()
        {
        }


        public static DelaunayTriangulation Triangulate(List<Vertex> vertices)
        {
            if (vertices.Count < 3)
                throw new ArgumentException("There must be at least three vertices to triangulate.");

            var triangulation = new DelaunayTriangulation();
            triangulation.Execute(vertices);
            return triangulation;
        }

        public static DelaunayTriangulation Triangulate(List<Vec2> points)
        {
            if (points.Count < 3)
                throw new ArgumentException("There must be at least three points to triangulate.");

            var triangulation = new DelaunayTriangulation();
            var vertices = points.Select(p => new Vertex(p)).Distinct();
            triangulation.Execute(vertices);
            return triangulation;
        }

        //divide and conquer:
        //order points by X and Y coordinate
        //split point set until each division has 2 or 3 points
        //triangulate each division and merge

        private void Execute(IEnumerable<Vertex> vertices)
        {
            var orderedVertices = vertices.OrderBy(p => p.Position.X).ThenBy(p => p.Position.Y).ToList();
            var triangles = DivideAndTriangulate(orderedVertices, 0, vertices.Count() - 1);
            
            _triangles = triangles.Where(t=>!Triangle.IsDegenerate(t)).ToList();
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

                return new List<Triangle> { triangle };
            }
            else if (pointsCount == 2)
            {
                var triangle = new Triangle(
                        vertices[startIndex + 0],
                        vertices[startIndex + 1]);

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

                var triangle = new Triangle(v0, v1, v2);
                triangles.Add(triangle);

                triangle.E0.RemoveDegenerateTriangles();
                triangle.E1.RemoveDegenerateTriangles();
                triangle.E2.RemoveDegenerateTriangles();

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

        //find bottom most convex hull edge that connects left and right side triangles
        private Edge FindBottomMostEdge(List<Triangle> leftTriangles, List<Triangle> rightTriangles)
        {
            if (leftTriangles.Count == 0 || rightTriangles.Count == 0)
                throw new InvalidOperationException("There should be at least one triangle on each list.");

            var vertices = new List<Tuple<Vertex, TrianglesDivideSide>>();

            Action<Vertex, TrianglesDivideSide> addVertex = (v, s) =>
            {
                if (v != null && !vertices.Any(t => t.Item1 == v))
                    vertices.Add(new Tuple<Vertex, TrianglesDivideSide>(v, s));
            };

            foreach (var triangle in leftTriangles)
            {
                addVertex(triangle.V0, TrianglesDivideSide.Left);
                addVertex(triangle.V1, TrianglesDivideSide.Left);
                addVertex(triangle.V2, TrianglesDivideSide.Left);
            }

            foreach (var triangle in rightTriangles)
            {
                addVertex(triangle.V0, TrianglesDivideSide.Right);
                addVertex(triangle.V1, TrianglesDivideSide.Right);
                addVertex(triangle.V2, TrianglesDivideSide.Right);
            }

            vertices = vertices.OrderBy(t => t.Item1.Position, new Vec2Comparer()).ToList();

            var minAngle = double.MaxValue;
            var minI = 0;
            var minK = 0;

            for (int i = 0; i < vertices.Count; i++)
            {
                var j = i + 1;
                var vertex0 = vertices[i];
                var vertex1 = vertices[j];
                var p0 = vertex0.Item1.Position;
                var p1 = vertex1.Item1.Position;
                var v0 = p1 - p0;

                minAngle = 1.0;
                minI = i;
                minK = j;

                for (int k = j+1; k < vertices.Count; k++)
                {
                    var p2 = vertices[k].Item1.Position;
                    var v1 = p2 - p0;
                    var angle = Vec2.Dot(Vec2.Normalize(v0), Vec2.Normalize(v1));
                    var sin = Vec2.Cross(v0, v1);

                    if (Compare.LessOrEqual(sin, 0.0, Compare.TOLERANCE) && 
                        Compare.Less(angle, minAngle, Compare.TOLERANCE))
                    { 
                        minAngle = angle;
                        minK = k;
                    }
                }

                if (vertices[minK].Item2 == TrianglesDivideSide.Right)
                    break;
            }

            var minJ = minI;

            for (int k = minK; k > minJ; k--)
            {
                var j = k - 1;
                var vertex0 = vertices[k];
                var vertex1 = vertices[j];
                var p0 = vertex0.Item1.Position;
                var p1 = vertex1.Item1.Position;
                var v0 = p1 - p0;

                minAngle = 1.0;
                minK = k;
                minI = j;

                for (int i = j-1; i >= minJ; i--)
                {
                    var p2 = vertices[i].Item1.Position;
                    var v1 = p2 - p0;
                    var angle = Vec2.Dot(Vec2.Normalize(v0), Vec2.Normalize(v1));
                    var sin = Vec2.Cross(v0, v1);

                    if (Compare.GreaterOrEqual(sin, 0.0, Compare.TOLERANCE) && 
                        Compare.Less(angle, minAngle, Compare.TOLERANCE))
                    {
                        minAngle = angle;
                        minI = i;
                    }
                }

                if (vertices[minI].Item2 == TrianglesDivideSide.Left)
                    break;
            }

            return new Edge(vertices[minI].Item1, vertices[minK].Item1);
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
                var findParams = BuildFindCandidateParams(side, baseEdge);
                var findResult = FindCandidate(findParams);

                if (findResult.Candidate == null)
                    break;

                var candidateEdge = findResult.CandidateEdge;
                candidate = findResult.Candidate;

                var nextPotentialCandidate = findResult.NextPotentialCandidate;

                var v0 = findParams.BaseEdgeDirection;
                var v1 = nextPotentialCandidate.Position - findParams.BaseVertex.Position;

                //consider only edges to the same side as we are finding the vertex
                if ((findParams.Side == TrianglesDivideSide.Right && 
                    Compare.Greater(Vec2.Cross(v0, v1), 0.0, Compare.TOLERANCE)) ||
                    (findParams.Side == TrianglesDivideSide.Left && 
                    Compare.Less(Vec2.Cross(v0, v1), 0.0, Compare.TOLERANCE)))
                    return candidate;

                //if found candidate, see if its circumcircle contains the next potential candidate
                //if it does not, then we are good to go
                var a = candidate.Position;
                var b = baseEdge.V0.Position;
                var c = baseEdge.V1.Position;
                var p = nextPotentialCandidate.Position;

                if (!Triangle.CircumcircleContainsPoint(a, b, c, p))
                    return candidate;
                
                DeleteEdge(candidateEdge, ref triangles);
                candidate = null;
            }

            return candidate;
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

            triangles.RemoveAll(t => t.Contains(edge) );

            edge.V0.RemoveEdge(edge);
            edge.V1.RemoveEdge(edge);
        }

        private FindCandidateParams BuildFindCandidateParams(
            TrianglesDivideSide side,
            Edge baseEdge)
        {
            var findParams = new FindCandidateParams();
            findParams.Side = side;
            findParams.BaseEdge = baseEdge;

            if (side == TrianglesDivideSide.Left)
            {
                findParams.AngleSign = 1.0;
                findParams.BaseVertex = baseEdge.V0;
                findParams.BaseEdgeDirection = baseEdge.V1.Position - baseEdge.V0.Position;
            }
            else
            {
                findParams.AngleSign = -1.0;
                findParams.BaseVertex = baseEdge.V1;
                findParams.BaseEdgeDirection = baseEdge.V0.Position - baseEdge.V1.Position;
            }

            findParams.Edges = findParams.BaseVertex.Edges.Where(e => e != baseEdge).ToList();

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
                Vec2 v1 = currentPotentialCandidate.Position - baseVertex.Position;
                var currentAngle = -Vec2.Dot(Vec2.Normalize(v0), Vec2.Normalize(v1));
                var cross = Vec2.Cross(v0, v1) * angleSign;
                var lessThan180 = Compare.Greater(cross, 0, Compare.TOLERANCE);
                var lessThanMinAngle = Compare.Less(currentAngle, minCandidateAngle, Compare.TOLERANCE);
                // if angle between base edge and candidate edge is the smallest angle and is less than 180 degrees
                if (lessThan180 && lessThanMinAngle)
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

