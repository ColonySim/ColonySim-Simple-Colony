using System;
namespace SimpleColony
{
    public enum Talent { Gathering, Defense, Fieldwork, Engineering }

    public sealed class SkillProfile
    {
        readonly int[] levels = new int[4];
        readonly int?[] overrides = new int?[8];

        internal SkillProfile(IRandomSource random)
        {
            for (int i = 0; i < levels.Length; i++) levels[i] = 1 + random.Next(10);
        }

        public Talent TalentFor(Profession role)
        {
            switch (role)
            {
                case Profession.Gatherer:
                case Profession.Supplier: return Talent.Gathering;
                case Profession.Defender:
                case Profession.Stabilizer: return Talent.Defense;
                case Profession.Scout:
                case Profession.Medic: return Talent.Fieldwork;
                case Profession.Researcher:
                case Profession.Builder: return Talent.Engineering;
                default: throw new ArgumentOutOfRangeException(nameof(role));
            }
        }
        public int Level(Profession role) => levels[(int)TalentFor(role)];
        public int Priority(Profession role) => overrides[(int)role] ?? (Level(role) + 1) / 2;
        public bool IsAutomatic(Profession role) => !overrides[(int)role].HasValue;
        public float WorkRate(Profession role) => 1.25f + Level(role) * .1f;
        internal void Set(Profession role, int? priority)
        {
            if ((int)role < 0 || (int)role >= overrides.Length || priority < 0 || priority > 5)
                throw new ArgumentOutOfRangeException();
            overrides[(int)role] = priority;
        }
    }
}
