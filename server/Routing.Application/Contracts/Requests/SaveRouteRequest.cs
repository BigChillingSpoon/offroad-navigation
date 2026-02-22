namespace Routing.Application.Contracts.Models
{
    public sealed record SaveRouteRequest
    {
        public required Guid Id { get; init; }
        //todo require - since I still dont handle users, do not require at the moment
        public Guid UserId { get; init; }
        public required string Name { get; init; }
        public required double StartLatitude { get; init; }
        public required double StartLongitude { get; init; }
        public required double EndLatitude { get; init; }
        public required double EndLongitude { get; init; }
        public required TimeSpan Duration { get; init; }
        public required double TotalDistanceMeters { get; init; }
        public required double OffroadDistanceMeters { get; init; }
        public required double ElevationGainMeters { get; init; }
        public required double ElevationLossMeters { get; init; }
    }
}
