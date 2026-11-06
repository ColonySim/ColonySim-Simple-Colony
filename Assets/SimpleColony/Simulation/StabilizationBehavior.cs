using System;
namespace SimpleColony
{
    public sealed class StabilizationBehavior : IProfessionBehavior
    {
        public bool CanWork(World world, Node node) => node.Known && node.Threat <= 0 && node.Fortification < Node.MaxFortification && world.HasEssence;
        public bool Work(World world, Pawn pawn, Node node, float dt)
        {
            if (pawn.CargoType != Resource.Stability || pawn.Cargo <= 0) return true;
            float essence = Math.Min(pawn.Cargo, Math.Min(dt * .6f, (Node.MaxFortification - node.Fortification) / 3));
            pawn.UseCargo(essence);
            node.Fortify(essence * 3);
            return pawn.Cargo <= .001f || node.Fortification >= Node.MaxFortification - .001f;
        }
    }
}
