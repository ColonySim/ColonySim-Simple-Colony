using System;
using System.Collections.Generic;

namespace SimpleColony
{
    internal sealed class JobCoordinator
    {
        readonly World world;
        readonly Pathfinder pathfinder;
        readonly IReadOnlyDictionary<Profession, IProfessionBehavior> behaviors;
        public JobCoordinator(World world, Pathfinder pathfinder, IReadOnlyDictionary<Profession, IProfessionBehavior> behaviors)
        {
            this.world = world;
            this.pathfinder = pathfinder;
            this.behaviors = behaviors;
        }
        int Reservations(int id) { int count = 0; foreach (var pawn in world.Pawns) if (pawn.Health > 0 && pawn.Target == id) count++; return count; }
        bool DefenseOccupied(int nodeId)
        {
            foreach (var pawn in world.Pawns)
                if (pawn.Role == Profession.Defender && pawn.Health > 0 && !pawn.PendingRole.HasValue && pawn.Target == nodeId) return true;
            return false;
        }
        internal void SelectWork(Pawn pawn)
        {
            int target = FindTarget(pawn, pawn.Role);
            if (target >= 0) pawn.Assign(target);
        }

        internal int FindTarget(Pawn pawn, Profession role)
        {
            int target = -1;
            float best = float.MaxValue;
            foreach (var node in world.Nodes)
            {
                if (node.Id == World.Home || node.Priority == 0 || !world.Travel.Accessible(node, role == Profession.Defender) || !behaviors[role].CanWork(world, node)) continue;
                if (role == Profession.Defender && pawn.DefenderMode == DefenderMode.Patrol && !node.Known && !world.Frontier(node.Id)) continue;
                // An assignment reserves the defense job immediately, including travel time.
                if (role == Profession.Defender && DefenseOccupied(node.Id)) continue;
                float score = (world.Travel.Length(world.Nodes[pawn.At], node) + Reservations(node.Id) * 4) / node.Priority;
                if (role == Profession.Stabilizer)
                {
                    foreach (int next in node.Links) if (world.Nodes[next].Threat > 0) { score /= 4; break; }
                }
                if (score >= best) continue;
                var route = pathfinder.Find(pawn.At, node.Id, role == Profession.Defender);
                if (route == null) continue;
                float food = 0, energy = 0; int previous = pawn.At;
                foreach (int next in route) { food += world.Travel.FoodCost(world.Nodes[previous], world.Nodes[next]); energy += world.Travel.EnergyCost(world.Nodes[previous], world.Nodes[next]); previous = next; }
                var returnRoute = pathfinder.Find(node.Id, World.Home, role == Profession.Defender);
                if (returnRoute == null) continue;
                previous = node.Id;
                foreach (int next in returnRoute) { food += world.Travel.FoodCost(world.Nodes[previous], world.Nodes[next]); energy += world.Travel.EnergyCost(world.Nodes[previous], world.Nodes[next]); previous = next; }
                if (pawn.Food < food + 20 || pawn.Energy < energy + 15) continue;
                best = score; target = node.Id;
            }
            return target;
        }
    }
}
