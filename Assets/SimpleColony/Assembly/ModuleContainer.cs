using UnityEngine;
namespace SimpleColony
{
    public sealed class ModuleContainer : MonoBehaviour
    {
        [Tooltip("Обязательны World, Control и Population. В активном слоте допускается одна версия.")]
        [SerializeField] ModuleSlot slot = ModuleSlot.World;
        public ModuleSlot Slot => slot;
    }
}
