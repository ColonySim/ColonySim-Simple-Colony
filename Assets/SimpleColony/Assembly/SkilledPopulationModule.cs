namespace SimpleColony
{
    public sealed class SkilledPopulationModule : SceneModule
    {
        public override ModuleSlot Slot => ModuleSlot.Population;
        public override string Description => "Пешки с навыками + автоматическое распределение + минимумы и приоритеты профессий + своё меню колонии.";

        public override void Install(SessionRecipe recipe)
        {
            // One selection installs both simulation rules and their management interface.
            // Factories create fresh state for every restart.
            recipe.CreatePopulation = () => new SkilledPawnModel();
            recipe.Workforce = (parent, font, world, names, inspect) =>
                parent.gameObject.AddComponent<SkilledWorkforcePanel>().Initialize(font, world, names, inspect);
        }
    }
}
