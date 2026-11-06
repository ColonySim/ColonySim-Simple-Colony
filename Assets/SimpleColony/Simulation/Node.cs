using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class Node
    {
        public const float MaxFortification = 30;
        public float Fortification { get; private set; }
        internal void Fortify(float amount) { Fortification = Math.Min(MaxFortification, Fortification + amount); }
        public ConstructionSite Construction { get; private set; }
        public bool HasWaystation => Construction != null && Construction.Complete && Construction.Definition.Type == BuildingType.Waystation && Threat <= 0;
        internal void StartBuilding(BuildingDefinition definition) { Construction = new ConstructionSite(definition); }
        internal void AdvanceBuilding(float dt)
        {
            if (Construction == null || Construction.Complete) return;
            Construction.Work(dt);
            if (!Construction.Complete) return;
            if (Construction.Definition.Type == BuildingType.Farm && Find(Resource.Food) == null) Add(new Deposit(Resource.Food, 60));
            if (Construction.Definition.Type == BuildingType.Quarry && Find(Resource.Matter) == null) Add(new Deposit(Resource.Matter, 60));
        }
        public int Id { get; }
        public float X { get; }
        public float Y { get; }
        public bool Known { get; private set; }
        public bool Blocked { get; private set; }
        public int Priority { get; private set; } = 1;
        public float Threat { get; private set; }
        public float Shield { get; private set; }
        readonly List<int> links = new List<int>();
        readonly List<Deposit> deposits = new List<Deposit>();
        public IReadOnlyList<int> Links => links;
        public IReadOnlyList<Deposit> Deposits => deposits;
        internal bool Expanded { get; private set; }
        internal void MarkExpanded() { Expanded = true; }
        internal void Reveal() { Known = true; }
        internal void CyclePriority() { Priority = (Priority + 1) % 4; }
        internal void ToggleAccess() { Blocked = !Blocked; }
        internal void Infect(float amount)
        {
            float absorbed = Math.Min(Fortification, amount);
            Fortification -= absorbed;
            Threat += amount - absorbed;
        }
        internal void GrowOutbreak() { Threat = Math.Min(35, Threat + .5f); }
        internal void Suppress(float amount) { Threat = Math.Max(0, Threat - amount); }
        internal void Protect() { Shield = 3; }
        internal void AgeShield(float dt) { Shield = Math.Max(0, Shield - dt); }
        internal Node(int id, float x, float y) { Id = id; X = x; Y = y; }
        internal void Link(int id) { if (!links.Contains(id)) links.Add(id); }
        internal void Add(Deposit deposit) { deposits.Add(deposit); }
        public Deposit Find(Resource type) => deposits.Find(d => d.Type == type);
    }
}
