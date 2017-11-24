using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    public class Vertex
    {
        private IComparer<Edge> _edgeComparer;

        public Vec2 Position;

        public List<Edge> Edges;

        public Vertex(Vec2 position)
        {
            Position = position;
            Edges = new List<Edge>();
            _edgeComparer = new EdgeAngleComparer(this);
        }

        public override bool Equals(Object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;

            Vertex p = (Vertex)obj;

            foreach (var edge in p.Edges)
                if (!Edges.Contains(edge))
                    return false;

            return Position == p.Position;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        public static bool operator ==(Vertex a, Vertex b)
        {
            if (object.ReferenceEquals(a, null))
                return object.ReferenceEquals(b, null);

            return a.Equals(b);
        }

        public static bool operator !=(Vertex a, Vertex b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}", Position.X, Position.Y);
        }

        public void AddEdge(Edge edge)
        {
            Edges.Add(edge);
            Edges.Sort(_edgeComparer);
        }

        public void RemoveEdge(Edge edge)
        {
            Edges.Remove(edge);
            Edges.Sort(_edgeComparer);
        }

        public Edge Find(Vertex v)
        {
            return Edges.FirstOrDefault(
                        e =>
                        (e.V0 == this && e.V1 == v) ||
                        (e.V0 == v && e.V1 == this));
        }
    }
}
