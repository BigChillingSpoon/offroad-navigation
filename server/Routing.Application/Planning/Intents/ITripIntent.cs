using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Intents
{
    public interface ITripIntent
    {
        Coordinate Start { get; }

        //kind of needed when caclulating the route/loop, not only for analysis
        public bool AllowPrivateRoads { get; init; }
        public bool AllowGates { get; init; }
    }
}
