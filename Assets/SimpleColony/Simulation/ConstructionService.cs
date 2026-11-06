namespace SimpleColony
{
    public sealed class ConstructionService
    {
        readonly World world;
        internal ConstructionService(World world) { this.world = world; }
        public bool CanBuild(int id, BuildingType type)
        {
            if (world.Lost || id <= World.Home || id >= world.Nodes.Count || (int)type < 0 || (int)type >= BuildingDefinition.All.Count) return false;
            var n = world.Nodes[id]; var d = BuildingDefinition.All[(int)type];
            return n.Known && !n.Blocked && n.Threat <= 0 && n.Construction == null &&
                world.Stores[Resource.Food] >= d.FoodCost && world.Stores[Resource.Matter] >= d.MatterCost;
        }
        public bool Build(int id, BuildingType type)
        {
            if (!CanBuild(id, type)) return false;
            var definition = BuildingDefinition.All[(int)type];
            world.Stores.Spend(Resource.Food, definition.FoodCost);
            world.Stores.Spend(Resource.Matter, definition.MatterCost);
            world.Nodes[id].StartBuilding(definition);
            return true;
        }
        internal void Step(float dt)
        {
            foreach (var n in world.Nodes)
            {
                if (n.Construction == null || !n.Construction.Complete || n.Threat > 0) continue;
                var type = n.Construction.Definition.Type;
                if (type == BuildingType.Farm) n.Find(Resource.Food).Regenerate(dt * .4f);
                if (type == BuildingType.Quarry) n.Find(Resource.Matter).Regenerate(dt * .15f);
            }
        }
    }
}
