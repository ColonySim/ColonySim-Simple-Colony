using System;
using UnityEngine;
namespace SimpleColony
{
    public sealed class SessionRecipe
    {
        public Func<IRandomSource, IWorldGraph> Generate { get; set; }
        public Action<GameObject> StartControls { get; set; }
        public PawnModel Population { get; set; }
        public Mechanics Mechanics { get; set; }
        public World CreateWorld(int seed) => new World(seed, Mechanics, null, Population, Generate);
    }
}
