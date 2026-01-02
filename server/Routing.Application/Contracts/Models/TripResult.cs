using Routing.Domain.Enums;

namespace Routing.Application.Contracts.Models
{
    public sealed record TripResult
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public TripType Type { get; init; }
        public required double TotalDistanceMeters { get; init; }
        public required double OffroadDistanceMeters { get; init; }
        public required TimeSpan Duration { get; init; }
        public required double ElevationGainMeters { get; init; }
    }
}
