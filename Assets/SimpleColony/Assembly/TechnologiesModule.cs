namespace SimpleColony
{
    public sealed class TechnologiesModule : SceneModule
    {
        public override ModuleSlot Slot => ModuleSlot.Research;
        public override void Install(SessionRecipe recipe) { recipe.Mechanics |= Mechanics.Research; }
    }
}
