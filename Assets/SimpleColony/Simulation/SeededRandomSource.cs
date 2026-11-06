using System;
namespace SimpleColony
{
    public sealed class SeededRandomSource : IRandomSource
    {
        readonly Random random;
        public SeededRandomSource(int seed) { random = new Random(seed); }
        public int Next(int maximum) => random.Next(maximum);
        public double NextDouble() => random.NextDouble();
    }
    public static class SessionSeed
    {
        static int sequence;
        public static int Create() => unchecked((int)DateTime.UtcNow.Ticks ^ System.Threading.Interlocked.Increment(ref sequence));
    }
}
