namespace Routing.Application.Contracts.Models
{
    public sealed record FindLoopsRequest
    {
        public required double StartLatitude { get; init; }
        public required double StartLongitude { get; init; }
        public required double MaxDriveDistanceKm { get; init; }
        public required double PreferredLengthKm { get; init; }
        public bool AllowPrivateRoads { get; init; }
        public bool AllowGates { get; init; }
    }
}
