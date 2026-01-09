using Routing.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Candidates.Models
{
    public sealed record TripPlanChunk
    {
        public required IReadOnlyList<Coordinate> Geometry { get; init; }
        public double DistanceMeters { get; init; }
        public double DurationSeconds { get; init; }
        public double OffroadDistanceMeters { get; init; }
        public double ElevationGainMeters { get; init; }
    }
}
