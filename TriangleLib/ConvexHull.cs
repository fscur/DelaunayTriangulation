using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    public static class ConvexHull
    {
        //https://www.youtube.com/results?search_query=graham+scan+algorithm
        public static List<Vertex> GrahamScan(List<Vertex> vertices)
        {
            try
            {

            
            if (vertices == null)
                throw new ArgumentNullException();

            if (vertices.Count < 4)
                return vertices;

            // sort vertices by position to get left-bottom most vertex
            vertices = vertices.OrderBy(v => v.Position, new Vec2Comparer()).ToList();
            
            //sort vertices by angle relative to left-bottom-most vertex
            var orderedVertices = StackVerticesByAngle(vertices);

            //compare two consecutive segments and
            //add vertex to the hull if they perform a left turn, otherwise, 
            //remove previous vertices until they perform a left turn
            //continue adding vertices until there is no more vertices to add

            var i = 1;
            var currentVertex = vertices[0];
            var nextVertex = orderedVertices.Pop();

            //first two vertices are always from the hull
            var hull = new List<Vertex>();
            hull.Add(currentVertex);

            var v0 = nextVertex.Position - currentVertex.Position;

            while (orderedVertices.Count > 0)
            {
                hull.Add(nextVertex);
                currentVertex = nextVertex;
                nextVertex = orderedVertices.Pop();

                var v1 = nextVertex.Position - currentVertex.Position;

                //performin right turn:
                while (i > 0 && Compare.Less(Vec2.Cross(v0, v1), 0.0))
                {
                    hull.RemoveAt(i--);

                    if (i > 0)
                        v0 = hull[i].Position - hull[i - 1].Position;
                    
                    v1 = nextVertex.Position - hull[i].Position;
                }

                v0 = v1;
                i++;
            }

            hull.Add(nextVertex);

            return hull;
            }
            catch (Exception ex)
            {
                if (ex != null)
                {
                    throw new Exception();
                }
            }
            return null;
        }

        private static Stack<Vertex> StackVerticesByAngle(List<Vertex> vertices)
        {
            Vec2 v0;
            Vec2 v1;
            Stack<Vertex> orderedVertices;

            var verticesAngles = new List<Tuple<double, Vertex>>();
            var referenceVertex = vertices[0];
            for (var k = 1; k < vertices.Count; k++)
            {
                var vertex = vertices[k];
                v0 = -Vec2.YAxis;
                v1 = vertex.Position - referenceVertex.Position;
                verticesAngles.Add(new Tuple<double, Vertex>(1.0-Vec2.Dot(Vec2.Normalize(v0), Vec2.Normalize(v1)), vertex));
            }

            orderedVertices = new Stack<Vertex>();
            var verticesAnglesTemp = verticesAngles.OrderByDescending(t => t.Item1);
            var temp = verticesAnglesTemp.Select(t => t.Item2);
            foreach (var vertex in temp)
                orderedVertices.Push(vertex);

            return orderedVertices;
        }
    }
}
