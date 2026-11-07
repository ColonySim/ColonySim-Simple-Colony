namespace SimpleColony
{
    // The selected module owns both recruitment economics and birth traits.
    public abstract class PawnModel
    {
        public virtual bool AutomaticWork => false;
        internal virtual float WorkRate(Pawn pawn) => 1f;
        internal virtual bool PrepareWork(Pawn pawn, World world, JobCoordinator jobs) => false;
        public abstract int Price { get; }
        public abstract SkillProfile AtBirth(int worldSeed, int pawnId);
    }

}
