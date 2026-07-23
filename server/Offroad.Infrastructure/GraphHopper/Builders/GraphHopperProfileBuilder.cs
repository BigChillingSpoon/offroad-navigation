using Routing.Application.Ports;
using Routing.Domain.Enums;
using Routing.Infrastructure.GraphHopper.DTOs;

namespace Routing.Infrastructure.GraphHopper.Builders
{
    public static class GraphHopperProfileBuilder
    {
        public static string ResolveProfileName(RoutingPreferences preferences)
        {
            return preferences.Balance switch
            {
                RouteBalance.Shortest => "offroad_shortest",
                RouteBalance.Balanced => "offroad_balanced",
                RouteBalance.MaxOffroad => "offroad_hardcore",
                _ => "offroad_balanced"
            };
        }

        public static GraphHopperCustomModel BuildCustomModel(RoutingPreferences preferences)
        {
            var customModel = new GraphHopperCustomModel();

            if (!preferences.AllowPrivateRoads)
            {
                customModel.Priority.Add(new PriorityStatement
                {
                    IfCondition = "road_access == PRIVATE || road_access == FORESTRY || road_access == AGRICULTURAL || road_access == CUSTOMERS",
                    MultiplyBy = 0.0
                });
                customModel.Priority.Add(new PriorityStatement
                {
                    IfCondition = "in_cz_parks == true && road_class == TRACK",
                    MultiplyBy = 0.0
                });
            }

            if (!preferences.AllowGates)
            {
                customModel.Priority.Add(new PriorityStatement
                {
                    IfCondition = "custom_barrier > 1", // 1 = none in GH
                    MultiplyBy = 0.000001
                });
            }

            return customModel;
        }
    }
}
