namespace Routing.Domain.ValueObjects
{
    public sealed record EncodedPolyline
    {
        public string Points { get; init; } = string.Empty;
        public double Multiplier { get; init; }
        public double ElevationMultiplier { get; init; }
        public bool HasElevation { get; init; }
    }
}
