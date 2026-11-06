using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class Pawn
    {
        bool evacuating;
        int patrolTarget = -1;
        public DefenderMode DefenderMode { get; private set; }
        internal void SetDefenderMode(DefenderMode mode)
        {
            DefenderMode = mode;
            CancelOrder();
            patrolTarget = -1;
            if (mode == DefenderMode.Guard) OrderedDestination = At;
        }
        internal void Evacuate() { evacuating = true; CancelOrder(); }
        public int OrderedDestination { get; private set; } = -1;
        internal void OrderMove(int node) { Release(); OrderedDestination = node; }
        internal void CancelOrder() { OrderedDestination = -1; Release(); }
        public int Id { get; private set; }
        public Profession Role { get; private set; }
        public Profession? PendingRole { get; private set; }
        public int At { get; private set; }
        public int Next { get; private set; } = -1;
        public int Target { get; private set; } = -1;
        public float Progress { get; private set; }
        public float Food { get; private set; } = 100;
        public float Energy { get; private set; } = 100;
        public float Health { get; private set; } = 100;
        public float Cargo { get; private set; }
        public Resource CargoType { get; private set; }
        public Activity State { get; private set; }
        internal float Work { get; private set; }

        internal Pawn(int id, Profession role) { Id = id; Role = role; }
        internal void Assign(int target) { Target = target; }
        internal void Release() { Target = -1; Work = 0; }
        internal void ConvertTo(Profession role)
        {
            CancelOrder();
            PendingRole = Role == role ? (Profession?)null : role;
            Release();
        }
        internal void Load(Resource resource, float amount) { CargoType = resource; Cargo = amount; }
        internal void UseCargo(float amount) { Cargo = Math.Max(0, Cargo - amount); }
        internal void TakeDamage(float amount) { Health = Math.Max(0, Health - amount); }
        void Go(World world, int goal, bool evacuating = false)
        {
            int next = world.Path(At, goal, evacuating || Role == Profession.Defender);
            if (next < 0 || next == At) { Release(); State = Activity.Idle; return; }
            float food = world.Travel.FoodCost(world.Nodes[At], world.Nodes[next]), energy = world.Travel.EnergyCost(world.Nodes[At], world.Nodes[next]);
            if (Food < food || Energy < energy) { Release(); State = Activity.Resting; return; }
            Food -= food; Energy -= energy; Next = next; Progress = 0; State = Activity.Moving;
        }
        internal void Step(World world, JobCoordinator jobs, IReadOnlyDictionary<Profession, IProfessionBehavior> behaviors, float dt)
        {
            if (Health <= 0) { Release(); State = Activity.Dead; return; }
            Food = Math.Max(0, Food - dt * .08f);
            if (Food <= 0) Health = Math.Max(0, Health - dt * .6f);
            if (Health <= 0) { Release(); State = Activity.Dead; return; }
            if (Next >= 0)
            {
                Progress += dt * .18f * world.Travel.SpeedMultiplier / world.Travel.Length(world.Nodes[At], world.Nodes[Next]);
                if (Progress >= 1) { At = Next; Next = -1; Progress = 0; }
                return;
            }
            if (At == World.Home)
            {
                if (Cargo > 0 && (Role != Profession.Stabilizer || CargoType != Resource.Stability || PendingRole.HasValue)) { world.Stores.Add(CargoType, Cargo); Cargo = 0; }
                if (PendingRole.HasValue) { Role = PendingRole.Value; PendingRole = null; Release(); }
                if (Food < 95 && world.Stores.Spend(Resource.Food, dt)) Food = Math.Min(100, Food + dt * 12);
                if (Health < 100 && world.Stores.Spend(Resource.Medicine, dt * .1f)) Health = Math.Min(100, Health + dt);
                if (Energy < 98 || (Food < 90 && world.Stores[Resource.Food] > 0)) { Energy = Math.Min(100, Energy + dt * world.RecoveryRate); State = Activity.Resting; Release(); return; }
            }
            if (At == World.Home) evacuating = false;
            if (world.Nodes[At].Blocked) evacuating = true;
            if (At != World.Home && evacuating)
            {
                Release();
                CancelOrder();
                Go(world, World.Home, true);
                return;
            }
            if (State == Activity.Resting && At != World.Home) Energy = Math.Min(100, Energy + dt);
            if (!behaviors.ContainsKey(Role) && !PendingRole.HasValue && Cargo <= 0)
            {
                Release();
                State = Activity.Idle;
                if (At != World.Home) Go(world, World.Home);
                return;
            }
            if ((Cargo > 0 && !(Role == Profession.Stabilizer && CargoType == Resource.Stability)) || PendingRole.HasValue || Food < 30 || Energy < 25 || (At != World.Home && world.NeedsResupply(this)) || Health < 35 || (world.Nodes[At].Threat > 0 && Role != Profession.Defender))
            { Release(); Go(world, World.Home); return; }
            if (OrderedDestination >= 0)
            {
                if (world.Nodes[OrderedDestination].Blocked || (world.Nodes[OrderedDestination].Threat > 0 && Role != Profession.Defender)) CancelOrder();
                else
                {
                    if (At != OrderedDestination) Go(world, OrderedDestination);
                    else if (Role == Profession.Defender && world.Nodes[At].Threat > 0)
                    {
                        State = Activity.Working;
                        behaviors[Role].Work(world,this,world.Nodes[At],dt);
                    }
                    else State = Activity.Guarding;
                    return;
                }
            }
            if (Role == Profession.Stabilizer && Cargo <= .001f)
            {
                Release();
                if (At != World.Home) { Go(world,World.Home); return; }
                float essence = Math.Min(world.CargoCapacity, world.Stores[Resource.Stability]);
                if (essence <= 0) { State = Activity.Idle; return; }
                world.Stores.Spend(Resource.Stability, essence);
                Load(Resource.Stability, essence);
            }
            if (Target >= 0 && (!behaviors[Role].CanWork(world, world.Nodes[Target]) || world.Nodes[Target].Priority == 0 || !world.Travel.Accessible(world.Nodes[Target], Role == Profession.Defender))) Release();
            if (Target < 0) jobs.SelectWork(this);
            if (Target < 0)
            {
                if (Role == Profession.Defender)
                {
                    if (DefenderMode == DefenderMode.Patrol)
                    {
                        if (patrolTarget == At) patrolTarget = -1;
                        if (patrolTarget < 0) patrolTarget = world.NextPatrolNode(this);
                        if (patrolTarget >= 0) { Go(world,patrolTarget); return; }
                    }
                    if (At != World.Home && (Energy < 98 || Food < 90) && (DefenderMode == DefenderMode.Patrol || world.HasThreats))
                    { Go(world,World.Home); return; }
                    State = Activity.Guarding;
                    return;
                }
                State = Activity.Idle;
                if (At != World.Home) Go(world, World.Home);
                return;
            }
            if (At != Target) { Go(world, Target); return; }
            State = Activity.Working; Work += dt;
            if (behaviors[Role].Work(world, this, world.Nodes[At], dt)) Release();
        }
    }
}
