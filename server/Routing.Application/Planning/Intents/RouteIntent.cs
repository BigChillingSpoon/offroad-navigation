using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Intents
{
    public sealed record RouteIntent : ITripIntent
    {
        public required Coordinate Start { get; init; }
        public required Coordinate End { get; init; }
        public RouteBalance Balance { get; init; } 
    }
}
