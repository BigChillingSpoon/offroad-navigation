using Routing.Domain.Exceptions;
using Routing.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Candidates.Models
{
    /// <summary>
    /// Represents whole route/loop 
    /// Contains informations that are important for trip as whole, such as Distance, Duration, Elevation etc.
    /// </summary>
    public sealed class TripCandidate
    {
        public double DistanceMeters { get; }
        public TimeSpan Duration { get; }
        public double OffroadDistanceMeters { get; }
        public double ElevationGainMeters { get; }
        public IReadOnlyList<Segment> Segments { get; init; }
        public IReadOnlyDictionary<string, object>? Metadata { get; init; }

        private TripCandidate(IReadOnlyList<Segment> segments, double distance, TimeSpan duration, double offroadDistance, double elevationGain)
        {
            Segments = segments;
            DistanceMeters = distance;
            Duration = duration;
            OffroadDistanceMeters = offroadDistance;
            ElevationGainMeters = elevationGain;
        }

        public static TripCandidate Create(IReadOnlyList<Segment> segments, double distance, TimeSpan duration, double offroadDistance, double elevationGain)
        {
            Validate(distance, duration, offroadDistance, elevationGain);
            return new TripCandidate(segments, distance, duration, offroadDistance, elevationGain);
        }
        private static void Validate(double distance, TimeSpan duration, double offroadDistance, double elevationGain)
        {
            //maybe return something different that domainException
            if (offroadDistance < 0) throw new DomainException("Offroad distance cannot be negative");
            if (elevationGain < 0) throw new DomainException("Elevation gain cannot be negative");
        }
    }
}
