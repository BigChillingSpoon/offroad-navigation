using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Intents
{
    public sealed record LoopIntent : ITripIntent
    {
        public required Coordinate Start { get; init; }
        public double PreferredLengthKm { get; init; }
        public double MaxDriveDistanceKm { get; init; }
    }
}
