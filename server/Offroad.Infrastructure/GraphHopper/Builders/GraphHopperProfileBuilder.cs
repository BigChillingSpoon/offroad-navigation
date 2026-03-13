using Routing.Application.Planning.Intents;
using Routing.Domain.Enums;
using Routing.Infrastructure.GraphHopper.DTOs;

namespace Routing.Infrastructure.GraphHopper.Builders
{
    public static class GraphHopperProfileBuilder
    {
        public static string ResolveProfileName(ITripIntent intent)
        {
            return intent switch
            {
                LoopIntent => "offroad_hardcore",
                RouteIntent route => route.Balance switch
                {
                    RouteBalance.Shortest => "offroad_shortest",
                    RouteBalance.Balanced => "offroad_balanced",
                    RouteBalance.MaxOffroad => "offroad_hardcore",
                    _ => "offroad_balanced"
                },
                _ => "car"
            };
        }

        public static GraphHopperCustomModel BuildCustomModel(ITripIntent intent)
        {
            var customModel = new GraphHopperCustomModel();

            if (!intent.AllowPrivateRoads)
            {
                customModel.Priority.Add(new PriorityStatement
                {
                    IfCondition = "road_access == PRIVATE || road_access == NO || road_access == FORESTRY || road_access == AGRICULTURAL",
                    MultiplyBy = 0.0
                });
                customModel.Access.Add(new AccessStatement
                {
                    IfCondition = "road_access == PRIVATE || road_access == NO || road_access == FORESTRY || road_access == AGRICULTURAL",
                    BaseValue = false
                });
                customModel.Priority.Add(new PriorityStatement
                {
                    IfCondition = "in_cz_parks == true && road_class == TRACK",
                    MultiplyBy = 0.0 
                });
            }

            return customModel;
        }
    }
}