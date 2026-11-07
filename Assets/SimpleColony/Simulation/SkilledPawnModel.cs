namespace SimpleColony
{
    public sealed class SkilledPawnModel : PawnModel
    {
        public override bool AutomaticWork => true;
        public override int Price => 50;
        public override SkillProfile AtBirth(int worldSeed, int pawnId)
        {
            // Separate stream: hiring a pawn must not change future map generation.
            return new SkillProfile(new SeededRandomSource(unchecked(worldSeed * 397 ^ pawnId)));
        }

        internal override float WorkRate(Pawn pawn) => pawn.Skills.WorkRate(pawn.Role);

        internal override bool PrepareWork(Pawn pawn, World world, JobCoordinator jobs)
        {
            if (pawn.OrderedDestination >= 0 || (pawn.Role == Profession.Defender && pawn.DefenderMode != DefenderMode.Pursuit)) return false;
            if (pawn.At == World.Home)
            {
                if (pawn.Cargo == 0 && (pawn.NeedsWorkReview || pawn.Target < 0))
                    ChooseWork(pawn, world, jobs);
                return false;
            }
            if (pawn.NeedsWorkReview || (pawn.Target < 0 && pawn.Cargo == 0))
            {
                pawn.ReturnToCore(world);
                return true;
            }
            return false;
        }

        void ChooseWork(Pawn pawn, World world, JobCoordinator jobs)
        {
            int best = 0;
            Profession chosen = Profession.Unemployed;
            int target = -1;
            for (int i = 0; i < 8; i++)
            {
                var role = (Profession)i;
                if (!world.Supports(role) || pawn.Skills.Priority(role) == 0) continue;
                if (role == Profession.Stabilizer && world.Stores[Resource.Stability] <= 0) continue;
                int rank = pawn.Skills.Priority(role) * 100 + pawn.Skills.Level(role);
                if (rank <= best) continue;
                int candidate = jobs.FindTarget(pawn, role);
                if (candidate < 0) continue;
                best = rank; chosen = role; target = candidate;
            }
            pawn.BeginAssignment(chosen, target);
        }
    }
}
