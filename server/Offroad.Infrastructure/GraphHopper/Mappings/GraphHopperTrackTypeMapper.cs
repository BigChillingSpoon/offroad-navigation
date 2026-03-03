using Routing.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Infrastructure.GraphHopper.Mappings
{
    internal static class GraphHopperTrackTypeMapper
    {
        public static TrackType Map(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return TrackType.UNKNOWN;

            return value.Trim().ToLowerInvariant() switch
            {
                "grade1" => TrackType.GRADE1,
                "grade2" => TrackType.GRADE2,
                "grade3" => TrackType.GRADE3,
                "grade4" => TrackType.GRADE4,
                "grade5" => TrackType.GRADE5,
                _ => TrackType.UNKNOWN
            };
        }
    }
}
