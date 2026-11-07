namespace SimpleColony
{
    public sealed class ColonyControlsModule : SceneModule
    {
        public override ModuleSlot Slot => ModuleSlot.Control;
        public override void Install(SessionRecipe recipe) { recipe.StartControls = root => root.AddComponent<ColonyView>(); }
    }
}
