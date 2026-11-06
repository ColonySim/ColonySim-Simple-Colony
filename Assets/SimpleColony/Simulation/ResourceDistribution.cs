namespace SimpleColony
{
    // Stratified blocks keep all resource types available without filling every territory.
    internal sealed class ResourceDistribution
    {
        readonly IRandomSource random;
        readonly int[] bag = new int[20];
        int remaining;
        public ResourceDistribution(IRandomSource random) { this.random = random; }
        public void Populate(Node node)
        {
            if (remaining == 0)
            {
                for (int i = 0; i < 20; i++) bag[i] = i < 11 ? -1 : i < 15 ? 0 : i - 14;
                for (int i = 19; i > 0; i--) { int j = random.Next(i + 1); int temp = bag[i]; bag[i] = bag[j]; bag[j] = temp; }
                remaining = 20;
            }
            int kind = bag[--remaining];
            if (kind < 0) return;
            node.Add(new Deposit((Resource)kind, 40 + random.Next(41)));
            if (random.NextDouble() < .2)
            {
                int second = (kind + 1 + random.Next(5)) % 6;
                node.Add(new Deposit((Resource)second, 20 + random.Next(21)));
            }
        }
    }
}
