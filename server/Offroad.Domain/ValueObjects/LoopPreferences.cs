namespace Routing.Domain.ValueObjects
{
    public sealed record LoopPreferences : RoutePreferences
    {
        public required double MaxDriveDistanceKm { get; init; }
        public required double PreferredLengthKm { get; init; }
    }
}
