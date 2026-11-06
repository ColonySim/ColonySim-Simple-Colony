namespace SimpleColony
{
    public sealed class BuildingBehavior : IProfessionBehavior
    {
        readonly GatheringBehavior gathering = new GatheringBehavior(Resource.Matter);
        public bool CanWork(World world, Node node) => node.Known && node.Threat <= 0 &&
            ((node.Construction != null && !node.Construction.Complete) || gathering.CanWork(world, node));
        public bool Work(World world, Pawn pawn, Node node, float dt)
        {
            if (node.Construction == null || node.Construction.Complete) return gathering.Work(world, pawn, node, dt);
            node.AdvanceBuilding(dt);
            return node.Construction.Complete;
        }
    }
}
