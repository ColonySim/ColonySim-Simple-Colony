using System;
using System.Collections.Generic;

namespace SimpleColony
{
    // Colony-wide policy. Personal preferences belong to SkillProfile.
    public sealed class ColonyWorkPlan
    {
        readonly int[] minimum = new int[8];
        readonly int[] priority = { 1, 1, 1, 1, 1, 1, 1, 1 };
        readonly Dictionary<int, Profession> reserved = new Dictionary<int, Profession>();
        int version, cachedVersion = -1, cachedAlive = -1;

        static bool Valid(Profession role) => (int)role >= 0 && (int)role < 8;
        public int Minimum(Profession role) => Valid(role) ? minimum[(int)role] : 0;
        public int Priority(Profession role) => Valid(role) ? priority[(int)role] : 0;

        public bool ChangeMinimum(World world, Profession role, int delta)
        {
            if (!CanEdit(world, role) || (delta != -1 && delta != 1)) return false;
            int total = 0;
            foreach (int value in minimum) total += value;
            int next = minimum[(int)role] + delta;
            if (next < 0 || (delta > 0 && total >= LivingCount(world))) return false;
            minimum[(int)role] = next;
            Changed(world);
            return true;
        }

        public bool SetPriority(World world, Profession role, int value)
        {
            if (!CanEdit(world, role) || value < 0 || value > 5) return false;
            if (priority[(int)role] == value) return true;
            priority[(int)role] = value;
            Changed(world);
            return true;
        }

        bool CanEdit(World world, Profession role) => world != null && !world.Lost &&
            ReferenceEquals((world.Population as SkilledPawnModel)?.WorkPlan, this) &&
            Valid(role) && world.Supports(role);

        static int LivingCount(World world)
        {
            int count = 0;
            foreach (var pawn in world.Pawns) if (pawn.Health > 0) count++;
            return count;
        }

        void Changed(World world)
        {
            version++;
            foreach (var pawn in world.Pawns) pawn.ReconsiderWork();
        }

        // Reservations stay stable until policy or population changes.
        internal void Refresh(World world)
        {
            int alive = LivingCount(world);
            if (cachedVersion == version && cachedAlive == alive) return;
            reserved.Clear();
            var roles = new List<Profession>();
            for (int i = 0; i < 8; i++)
                if (world.Supports((Profession)i)) roles.Add((Profession)i);
            // When minima compete for a specialist, urgency wins first.
            roles.Sort((a, b) => Priority(a) != Priority(b)
                ? Priority(b).CompareTo(Priority(a)) : a.CompareTo(b));
            foreach (var role in roles)
            {
                for (int place = 0; place < Minimum(role); place++)
                {
                    Pawn best = null;
                    foreach (var candidate in world.Pawns)
                    {
                        if (candidate.Health <= 0 || reserved.ContainsKey(candidate.Id)) continue;
                        if (best == null || candidate.Skills.Level(role) > best.Skills.Level(role))
                            best = candidate;
                    }
                    if (best == null) break;
                    reserved.Add(best.Id, role);
                }
            }
            cachedVersion = version;
            cachedAlive = alive;
            foreach (var pawn in world.Pawns) pawn.ReconsiderWork();
        }

        internal Profession? ReservedRole(World world, Pawn pawn)
        {
            Refresh(world);
            return reserved.TryGetValue(pawn.Id, out var role) ? role : (Profession?)null;
        }
    }
}
