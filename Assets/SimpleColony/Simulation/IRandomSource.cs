namespace SimpleColony
{
    public interface IRandomSource
    {
        int Next(int maximum);
        double NextDouble();
    }
}
