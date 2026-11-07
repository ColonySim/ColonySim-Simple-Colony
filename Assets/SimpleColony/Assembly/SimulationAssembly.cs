using System;
using System.Collections.Generic;
using UnityEngine;
namespace SimpleColony
{
    [DisallowMultipleComponent]
    public sealed class SimulationAssembly : MonoBehaviour
    {
        SessionRecipe running;
        public SessionRecipe Running => running ?? (running = Compose());

        public SessionRecipe Compose()
        {
            var installed = new ModuleSelection();
            var recipe = new SessionRecipe();
            foreach (var module in GetComponentsInChildren<SceneModule>(true))
            {
                if (!module.isActiveAndEnabled) continue;
                var container = module.GetComponent<ModuleContainer>();
                if (!container) throw new InvalidOperationException(module.name + ": отсутствует контейнер слота.");
                installed.Register(container.Slot, module.Slot);
                module.Install(recipe);
            }
            installed.Validate();
            if (recipe.Generate == null || recipe.StartControls == null || recipe.Population == null)
                throw new InvalidOperationException("Обязательный модуль не предоставил реализацию.");
            return recipe;
        }
        void Start()
        {
            var recipe = Running;
            if (!GetComponent<ColonyView>()) recipe.StartControls(gameObject);
        }
    }
}
