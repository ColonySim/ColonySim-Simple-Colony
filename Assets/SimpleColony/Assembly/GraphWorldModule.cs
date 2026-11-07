namespace SimpleColony
{
    public sealed class GraphWorldModule : SceneModule
    {
        public override ModuleSlot Slot => ModuleSlot.World;
        public override void Install(SessionRecipe recipe) { recipe.Generate = random => new WorldGraph(random); }
    }
}
