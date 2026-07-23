using Routing.Application.Planning.Intents;
using Routing.Domain.Enums;

namespace Routing.Application.Ports
{
    public static class RoutingPreferencesMappings
    {
        public static RoutingPreferences ToRoutingPreferences(this RouteIntent intent) => new()
        {
            Balance = intent.Balance,
            AllowPrivateRoads = intent.AllowPrivateRoads,
            AllowGates = intent.AllowGates
        };

        // Loops always want maximum offroad character - not a user choice, a property of what a "loop" is.
        public static RoutingPreferences ToRoutingPreferences(this LoopIntent intent) => new()
        {
            Balance = RouteBalance.MaxOffroad,
            AllowPrivateRoads = intent.AllowPrivateRoads,
            AllowGates = intent.AllowGates
        };
    }
}
