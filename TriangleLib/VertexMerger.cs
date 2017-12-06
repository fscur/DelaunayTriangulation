using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    public class MergeNode
    {
        private Vertex _vertex;
        private List<MergeNode> _children = new List<MergeNode>();
        private double _weight = 0;

        public MergeNode(Vertex vertex)
        {
            _vertex = vertex;
        }

        public void AddChild(MergeNode child, double weight)
        {
            _children.Add(child);
            _weight += weight;
        }

        public Vertex Vertex
        {
            get { return _vertex; }
        }

        public double Weight
        {
            get { return _weight; }
        }

        public List<MergeNode> Children
        {
            get { return _children; }
        }
    }

    public static class VertexMerger
    {
        public static Dictionary<Vertex, List<Vertex>> Merge(List<Vertex> vertices, double tolerance)
        {
            var nodes = new Dictionary<int, MergeNode>();
            var length = vertices.Count();

            for (int i = 0; i < length; i++)
            {
                var p0 = vertices[i];

                MergeNode node;

                if (nodes.ContainsKey(i))
                    node = nodes[i];
                else
                {
                    node = new MergeNode(p0);
                    nodes.Add(i, node);
                }

                for (int j = i + 1; j < length; j++)
                {
                    var p1 = vertices[j];
                    var distance = Vec2.Length(p0.Position - p1.Position);

                    MergeNode childNode;

                    if (nodes.ContainsKey(j))
                        childNode = nodes[j];
                    else
                    {
                        childNode = new MergeNode(p1);
                        nodes.Add(j, childNode);
                    }

                    if (Compare.Less(distance, 2.0 * tolerance, Compare.TOLERANCE))
                    {
                        node.AddChild(childNode, distance);
                        childNode.AddChild(node, distance);
                    }
                }
            }

            var orderedNodes = nodes.Values.ToList().OrderByDescending(p => p.Weight).ToList();
            var mergedVertices = new Dictionary<Vertex, List<Vertex>>();

            while (orderedNodes.Count > 0)
            {
                var node = orderedNodes[0];
                var mergedNodes = node.Children.Count + 1;

                var position = node.Vertex.Position;

                var closePoints = new List<Vertex>();
                closePoints.Add(node.Vertex);

                var children = node.Children;

                foreach (var child in children)
                {
                    position += child.Vertex.Position;
                    orderedNodes.Remove(child);
                    closePoints.Add(child.Vertex);
                }

                mergedVertices.Add(new Vertex(position / mergedNodes), closePoints);
                orderedNodes.Remove(node);
            }

            return mergedVertices;
        }
    }
}
