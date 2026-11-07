namespace SimpleColony
{
    public sealed class OutbreaksModule : SceneModule
    {
        public override ModuleSlot Slot => ModuleSlot.Threats;
        public override void Install(SessionRecipe recipe) { recipe.Mechanics |= Mechanics.Threats; }
    }
}
