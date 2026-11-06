using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class WorldGraph
    {
        readonly List<Node> nodes = new List<Node>();
        readonly IRandomSource random;
        readonly ResourceDistribution distribution;
        public IReadOnlyList<Node> Nodes { get; }
        public int Revision { get; private set; }
        internal WorldGraph(IRandomSource random)
        {
            this.random = random; distribution = new ResourceDistribution(random);
            Nodes = nodes.AsReadOnly();
            nodes.Add(new Node(0, 0, 0)); nodes[0].Reveal(); Expand(0);
            int initial = nodes.Count;
            for (int i = 1; i < initial; i++) { nodes[i].Reveal(); Expand(i); }
            if (nodes[2].Find(Resource.Matter) == null) nodes[2].Add(new Deposit(Resource.Matter, 70));
            if (nodes[3].Find(Resource.Knowledge) == null) nodes[3].Add(new Deposit(Resource.Knowledge, 50));
            if (nodes[1].Find(Resource.Food) == null) nodes[1].Add(new Deposit(Resource.Food, 80));
        }
        void Connect(int a, int b) { nodes[a].Link(b); nodes[b].Link(a); Revision++; }
        void Expand(int id)
        {
            var origin = nodes[id]; if (origin.Expanded) return; origin.MarkExpanded();
            int directions = id == World.Home ? 6 : 3;
            double offset = random.NextDouble() * Math.PI * 2;
            for (int i = 0; i < directions; i++)
            {
                double angle = offset + Math.PI * 2 * i / directions;
                float x = origin.X + (float)Math.Cos(angle) * 1.8f, y = origin.Y + (float)Math.Sin(angle) * 1.8f;
                Node nearby = nodes.Find(n => n.Id != id && (n.X - x) * (n.X - x) + (n.Y - y) * (n.Y - y) < 1.3f);
                if (nearby != null) { Connect(id, nearby.Id); continue; }
                var node = new Node(nodes.Count, x, y);
                distribution.Populate(node);
                nodes.Add(node); Connect(id, node.Id);
            }
        }
        public void Discover(int id) { nodes[id].Reveal(); Expand(id); Revision++; }
        public bool Frontier(int id) { foreach (int next in nodes[id].Links) if (nodes[next].Known) return true; return false; }
        public int Edges() { int count = 0; foreach (var node in nodes) count += node.Links.Count; return count / 2; }
    }
}
