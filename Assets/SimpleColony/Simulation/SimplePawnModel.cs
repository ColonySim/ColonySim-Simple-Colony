namespace SimpleColony
{
    public sealed class SimplePawnModel : PawnModel
    {
        public override int Price => 25;
        public override SkillProfile AtBirth(int worldSeed, int pawnId) => null;
    }
}
