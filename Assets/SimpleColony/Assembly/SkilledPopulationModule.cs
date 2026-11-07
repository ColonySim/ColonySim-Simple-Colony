namespace SimpleColony
{
    public sealed class SkilledPopulationModule : SceneModule
    {
        public override ModuleSlot Slot => ModuleSlot.Population;
        public override void Install(SessionRecipe recipe) { recipe.Population = new SkilledPawnModel(); }
    }
}
