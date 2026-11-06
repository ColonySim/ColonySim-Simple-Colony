namespace SimpleColony
{
    public sealed class SupplyGatheringBehavior : IProfessionBehavior
    {
        public bool CanWork(World world, Node node) => node.Known && node.Threat <= 0 &&
            ((node.Find(Resource.Charge)?.Amount ?? 0) > 0 || (node.Find(Resource.Stability)?.Amount ?? 0) > 0);
        public bool Work(World world, Pawn pawn, Node node, float dt)
        {
            if (pawn.Work < 4) return false;
            var essence = node.Find(Resource.Stability);
            var charge = node.Find(Resource.Charge);
            var selected = essence != null && essence.Amount > 0 &&
                (charge == null || charge.Amount <= 0 || world.Stores[Resource.Stability] <= world.Stores[Resource.Charge]) ? essence : charge;
            pawn.Load(selected.Type, selected.Take(world.CargoCapacity));
            return true;
        }
    }
}
