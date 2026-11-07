using System.Collections.Generic;
namespace SimpleColony
{
    public interface IWorldGraph
    {
        IReadOnlyList<Node> Nodes { get; }
        int Revision { get; }
        void Discover(int id);
        bool Frontier(int id);
        int Edges();
    }
}
