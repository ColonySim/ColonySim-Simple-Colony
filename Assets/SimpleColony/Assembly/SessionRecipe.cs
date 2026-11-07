using System;
using UnityEngine;
namespace SimpleColony
{
    public delegate void WorkforcePanelFactory(Transform parent, Font font, Func<World> world,
        string[] professionNames, Action<Profession> inspect);

    public sealed class SessionRecipe
    {
        public Func<IRandomSource, IWorldGraph> Generate { get; set; }
        public Action<GameObject> StartControls { get; set; }
        public WorkforcePanelFactory Workforce { get; set; }
        public Func<PawnModel> CreatePopulation { get; set; }
        public Mechanics Mechanics { get; set; }
        public World CreateWorld(int seed) => new World(seed, Mechanics, null, CreatePopulation(), Generate);
    }
}
