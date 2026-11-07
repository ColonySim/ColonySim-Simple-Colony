using UnityEngine;
namespace SimpleColony
{
    public abstract class SceneModule : MonoBehaviour
    {
        public abstract ModuleSlot Slot { get; }
        public virtual string Description => "";
        public abstract void Install(SessionRecipe recipe);
    }
}
