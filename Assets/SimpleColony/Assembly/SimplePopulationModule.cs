namespace SimpleColony
{
    public sealed class SimplePopulationModule : SceneModule
    {
        public override ModuleSlot Slot => ModuleSlot.Population;
        public override string Description => "Простые пешки + ручное распределение профессий + меню ручного управления колонией.";

        public override void Install(SessionRecipe recipe)
        {
            // One selection installs both simulation rules and their management interface.
            // Factories create fresh state for every restart.
            recipe.CreatePopulation = () => new SimplePawnModel();
            recipe.Workforce = (parent, font, world, names, inspect) =>
                parent.gameObject.AddComponent<WorkforcePanel>().Initialize(font, world, names, inspect);
        }
    }
}
