using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    //planar straight line graph
    public class PSLG
    {
        private List<Vertex> _vertices;
        public List<Vertex> Vertices
        {
            get { return _vertices; }
        }

        private List<Edge> _edges;
        public List<Edge> Edges
        {
            get { return _edges; }
        }

        public PSLG()
        {
            _edges = new List<Edge>();
            _vertices = new List<Vertex>();
        }

        public bool Contains(Edge edge)
        {
            return Find(edge.V0, edge.V1) != null;
        }

        public Edge Find(Edge edge)
        {
            return Find(edge.V0, edge.V1);
        }

        public Edge Find(Vertex v0, Vertex v1)
        {
            return _edges.FirstOrDefault(e => (e.V0.Position == v0.Position && e.V1.Position == v1.Position) || (e.V0.Position == v1.Position && e.V1.Position == v0.Position));
        }

        public void RemoveEdge(Edge edge)
        {
            var e = Find(edge);
            if (e != null)
                _edges.Remove(e);

            //TODO: remover vertices?
            _vertices.Remove(edge.V0);
            _vertices.Remove(edge.V1);
        }

        public void AddEdge(Edge edge)
        {
            if (edge.V0 == edge.V1 || Contains(edge))
                return;
            
            _edges.Add(edge);
            AddVertex(edge.V0);
            AddVertex(edge.V1);
        }

        internal void AddVertex(Vertex vertex)
        {
            if (vertex != null && !_vertices.Contains(vertex))
                _vertices.Add(vertex);
        }
    }
}
