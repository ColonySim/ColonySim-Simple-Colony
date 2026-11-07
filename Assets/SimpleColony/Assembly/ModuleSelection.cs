using System;
using System.Collections.Generic;
namespace SimpleColony
{
    public enum ModuleSlot { World, Control, Population, Threats, Research }

    public sealed class ModuleSelection
    {
        readonly HashSet<ModuleSlot> occupied = new HashSet<ModuleSlot>();

        public void Register(ModuleSlot container, ModuleSlot provided)
        {
            if (container != provided)
                throw new InvalidOperationException($"Модуль {provided} помещён в контейнер {container}.");
            if (!occupied.Add(provided))
                throw new InvalidOperationException("Две активные реализации в слоте " + provided);
        }

        public void Validate()
        {
            foreach (var required in new[] { ModuleSlot.World, ModuleSlot.Control, ModuleSlot.Population })
                if (!occupied.Contains(required))
                    throw new InvalidOperationException("Пустой обязательный слот " + required);
        }
    }
}
