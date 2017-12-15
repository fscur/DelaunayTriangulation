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
        public bool Used { get; set; }
        public MergeNode(Vertex vertex)
        {
            _vertex = vertex;
            ShouldMerge = true;
        }

        public bool IsRoot
        {
            get; set;
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

        public double Weight { get { return _weight; } }

        public List<MergeNode> Children
        {
            get { return _children; }
        }

        public override string ToString()
        {
            return _vertex.ToString() + " - " + _weight;
        }
        
        public bool ShouldMerge { get; set; }
    }

    public static class VertexMerger
    {
        public static Dictionary<Vertex, List<Vertex>> Merge2(List<Vertex> vertices, double tolerance)
        {
            return null;
        }

        public static Dictionary<Vertex, List<Vertex>> Merge(List<Vertex> vertices, double tolerance)
        {
            MergeNode rootNode = new MergeNode(new Vertex(Vec2.Zero));
            rootNode.IsRoot = true;
            foreach (var vertex in vertices)
            {
                rootNode.AddChild(new MergeNode(vertex), 0);
            }
            
            while (rootNode.ShouldMerge)
                rootNode = Merge(rootNode, tolerance);

            var mergedVertices = new Dictionary<Vertex, List<Vertex>>();

            foreach (var child in rootNode.Children)
                if (!mergedVertices.ContainsKey(child.Vertex))
                    mergedVertices.Add(child.Vertex, child.Children.Select(m => m.Vertex).ToList());

            return mergedVertices;
        }

        public static MergeNode Merge(MergeNode node, double tolerance)
        {
            node.ShouldMerge = false;

            var length = node.Children.Count;
            for (int i = 0; i < length-1; i++)
            {
                var child0 = node.Children[i];

                for (int j = i+1; j < length; j++)
                {
                    var child1 = node.Children[j];

                    var p0 = child0.Vertex.Position;
                    var p1 = child1.Vertex.Position;

                    var distance = Vec2.Length(p0 - p1);

                    if (Compare.Less(distance, 2.0 * tolerance, Compare.TOLERANCE))
                    {
                        var weight = 1.0 / (distance * distance);

                        child0.AddChild(child1, weight);
                        child1.AddChild(child0, weight);
                        node.ShouldMerge = true;
                    }
                }
            }

            var ordered = node.Children.Where(c=>c.Children.Count > 0).OrderByDescending(c => c.Weight).ToList();
            try
            {
                while (ordered.Count() > 0)
                {
                    var child = ordered[0];

                    var position = child.Vertex.Position;
                    var childrenMerged = 1;
                    var children = child.Children.Where(c => !c.Used);
                    foreach (var n in children)
                    {
                        position += n.Vertex.Position;
                        childrenMerged++;
                        node.Children.Remove(n);
                        n.Used = true;
                    }
                    child.Used = true;
                    var mergedNode = new MergeNode(new Vertex(position / childrenMerged));
                    mergedNode.Children.AddRange(child.Children);
                    mergedNode.AddChild(child, 0);
                    node.Children.Remove(child);
                    node.AddChild(mergedNode, 0);

                    ordered = node.Children.Where(w => w.Weight > 0).OrderByDescending(c => c.Weight).ToList();
                }
            }
            catch (Exception ex)
            {

                return null;
            }
            

            return node;
        }
    }
}
