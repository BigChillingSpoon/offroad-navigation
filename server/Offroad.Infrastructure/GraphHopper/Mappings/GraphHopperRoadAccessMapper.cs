using Routing.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Infrastructure.GraphHopper.Mappings
{
    internal class GraphHopperRoadAccessMapper
    {
        public static RoadAccessType Map(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return RoadAccessType.Unknown;

            return value.ToLowerInvariant() switch
            {
                "forestry" => RoadAccessType.Forestry,
                "agricultural" => RoadAccessType.Agricultural,
                "customers" => RoadAccessType.Customers,
                "destination" => RoadAccessType.Destination,
                "delivery" => RoadAccessType.Delivery,
                "private" => RoadAccessType.Private,
                "yes" => RoadAccessType.Yes,
                "no" => RoadAccessType.No,
                _ => RoadAccessType.Unknown
            };
        }
    }
}
