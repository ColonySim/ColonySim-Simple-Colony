using System;
namespace SimpleColony
{
    public sealed class ConstructionSite
    {
        public BuildingDefinition Definition { get; }
        public float Progress { get; private set; }
        public bool Complete => Progress >= BuildingDefinition.WorkRequired;
        internal ConstructionSite(BuildingDefinition definition) { Definition = definition; }
        internal void Work(float dt) { Progress = Math.Min(BuildingDefinition.WorkRequired, Progress + dt); }
    }
}
