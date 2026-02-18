using Routing.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Infrastructure.GraphHopper.Mappings
{
    internal static class GraphHopperRoadClassMapper
    {
        public static RoadClassType Map(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return RoadClassType.Unknown;

            var v = value.Trim().ToLowerInvariant();
            //log unknown values - whether value is not empty but not recognized
            return value switch
            {
                "motorway" => RoadClassType.Motorway,
                "primary" => RoadClassType.Primary,
                "secondary" => RoadClassType.Secondary,
                "tertiary" => RoadClassType.Tertiary,
                "residential" => RoadClassType.Residential,
                "track" => RoadClassType.Track,
                _ => RoadClassType.Unknown
            };
        }
    }
}
