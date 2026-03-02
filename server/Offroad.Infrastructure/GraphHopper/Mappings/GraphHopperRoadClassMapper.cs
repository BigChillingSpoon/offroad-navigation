using Routing.Domain.Enums;
using System;

namespace Routing.Infrastructure.GraphHopper.Mappings
{
    internal static class GraphHopperRoadClassMapper
    {
        public static RoadClassType Map(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return RoadClassType.UNKNOWN;

            var v = value.Trim().ToLowerInvariant();

            return v switch
            {
                "motorway" => RoadClassType.MOTORWAY,
                "trunk" => RoadClassType.TRUNK,
                "primary" => RoadClassType.PRIMARY,
                "secondary" => RoadClassType.SECONDARY,
                "tertiary" => RoadClassType.TERTIARY,
                "residential" => RoadClassType.RESIDENTIAL,
                "unclassified" => RoadClassType.UNCLASSIFIED,
                "service" => RoadClassType.SERVICE,
                "road" => RoadClassType.ROAD,
                "track" => RoadClassType.TRACK,
                "bridleway" => RoadClassType.BRIDLEWAY,
                "steps" => RoadClassType.STEPS,
                "cycleway" => RoadClassType.CYCLEWAY,
                "path" => RoadClassType.PATH,
                "living_street" => RoadClassType.LIVING_STREET,
                "footway" => RoadClassType.FOOTWAY,
                "pedestrian" => RoadClassType.PEDESTRIAN,
                "platform" => RoadClassType.PLATFORM,
                "corridor" => RoadClassType.CORRIDOR,
                "construction" => RoadClassType.CONSTRUCTION,
                "busway" => RoadClassType.BUSWAY,
                "other" => RoadClassType.OTHER,
                _ => RoadClassType.UNKNOWN
            };
        }
    }
}