using UnityEngine;
namespace SimpleColony
{
    public abstract class SceneModule : MonoBehaviour
    {
        public abstract ModuleSlot Slot { get; }
        public abstract void Install(SessionRecipe recipe);
    }
}
