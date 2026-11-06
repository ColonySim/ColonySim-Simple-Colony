using System;
using System.Collections.Generic;
namespace SimpleColony
{
    public enum BuildingType { Farm, Quarry, Waystation }
    public sealed class BuildingDefinition
    {
        public BuildingType Type { get; }
        public string Name { get; }
        public string Description { get; }
        public int FoodCost { get; }
        public int MatterCost { get; }
        public string Icon { get; }
        public const float WorkRequired = 15;
        BuildingDefinition(BuildingType type, string name, string description, int food, int matter, string icon)
        { Type = type; Name = name; Description = description; FoodCost = food; MatterCost = matter; Icon = icon; }
        public static IReadOnlyList<BuildingDefinition> All { get; } = Array.AsReadOnly(new[] {
            new BuildingDefinition(BuildingType.Farm, "Ферма", "Создаёт запас питания и восстанавливает его. Собиратель доставляет урожай в ядро.", 10, 8, "food"),
            new BuildingDefinition(BuildingType.Quarry, "Мастерская", "Создаёт возобновляемый запас материи для дальнейшей стройки.", 8, 12, "matter"),
            new BuildingDefinition(BuildingType.Waystation, "Путевой пост", "Снижает расход еды и сил на соседних дорогах на 30%.", 15, 15, "road")
        });
    }
}
