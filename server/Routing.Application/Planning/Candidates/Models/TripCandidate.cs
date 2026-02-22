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
        public double TotalDistanceMeters { get; }
        public TimeSpan Duration { get; }
        public double OffroadDistanceMeters { get; }
        public double ElevationGainMeters { get; }
        public double ElevationLossMeters { get; }
        public IReadOnlyList<Segment> Segments { get; init; }
        public IReadOnlyDictionary<string, object>? Metadata { get; init; }

        private TripCandidate(IReadOnlyList<Segment> segments, double totalDistance, TimeSpan duration, double offroadDistance, double elevationGain, double elevationLoss)
        {
            Segments = segments;
            TotalDistanceMeters = totalDistance;
            Duration = duration;
            OffroadDistanceMeters = offroadDistance;
            ElevationGainMeters = elevationGain;
            ElevationLossMeters = elevationLoss;
        }

        public static TripCandidate Create(IReadOnlyList<Segment> segments, double totalDistance, TimeSpan duration, double elevationGain, double elevationLoss)
        {
            var offroadDistance = segments.Where(s => s.IsOffroad).Sum(s => s.DistanceMeters);
            Validate(totalDistance, duration, offroadDistance, elevationGain, elevationLoss);
            return new TripCandidate(segments, totalDistance, duration, offroadDistance, elevationGain, elevationLoss);
        }
        private static void Validate(double totalDistance, TimeSpan duration, double offroadDistance, double elevationGain, double elevationLoss)
        {
            if(offroadDistance < 0)
               throw new DomainException("Offroad distance cannot be negative");
            if(elevationGain < 0) 
                throw new DomainException("Elevation gain cannot be negative");
            if(elevationLoss < 0) 
                throw new DomainException("Elevation loss cannot be negative"); 
            if(totalDistance < 0)
                throw new DomainException("Total distance cannot be negative.");
        }
    }
}
