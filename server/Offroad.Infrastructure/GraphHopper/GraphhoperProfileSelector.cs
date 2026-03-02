using Routing.Application.Planning.Intents;
using Routing.Domain.Enums;

namespace Routing.Application.Planning.Candidates
{
    public static class GraphhoperProfileSelector
    {
        //from profile to graphhoper profile string
        public static string ToGraphhoperProfile(this LoopIntent intent)
        {
            return "offroad_hardcore"; // for loops there will always be max offroad
        }
        public static string ToGraphhopperProfile(this RouteIntent intent)
        {
            switch (intent.Balance)
            {
                case RouteBalance.Shortest:
                    return "car";
                case RouteBalance.Balanced:
                    return "offroad_balanced";
                case RouteBalance.MaxOffroad:
                    return "offroad_hardcore";
            }
            return "offroad_balanced";
        }
    }
}
