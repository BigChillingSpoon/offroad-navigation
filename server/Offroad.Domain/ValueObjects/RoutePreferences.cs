namespace Routing.Domain.ValueObjects
{
    public record RoutePreferences
    {
        public bool AllowPrivateRoads { get; init; }
        public bool AllowGates { get; init; }
    }
}
