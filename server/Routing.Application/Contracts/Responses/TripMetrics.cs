using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Contracts.Responses
{
    public sealed record TripMetrics
    {
        public required double TotalDistanceMeters { get; init; }
        public required double OffroadDistanceMeters { get; init; }
        public required TimeSpan Duration { get; init; }
        public required double ElevationGainMeters { get; init; }
    }
}
