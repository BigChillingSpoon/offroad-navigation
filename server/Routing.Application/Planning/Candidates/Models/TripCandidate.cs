using Routing.Domain.Enums;
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
    public class TripCandidate
    {
        public double TotalDistanceMeters { get; }
        public TimeSpan Duration { get; }
        public double OffroadDistanceMeters { get; }
        public double OffroadRatio => TotalDistanceMeters > 0 ? OffroadDistanceMeters / TotalDistanceMeters : 0;
        public double ElevationGainMeters { get; }
        public double ElevationLossMeters { get; }
        public double MaxGradientPercentage { get; }
        public EncodedPolyline Polyline { get; }
        public IReadOnlyList<Segment> Segments { get; }
        public IReadOnlyList<RoadBarrier> Barriers { get; }
        public IReadOnlyList<Interval<RestrictionType>> RestrictedZones { get; }
        public IReadOnlyDictionary<string, object>? Metadata { get; init; }

        protected TripCandidate(IReadOnlyList<Segment> segments, IReadOnlyList<RoadBarrier> barriers, IReadOnlyList<Interval<RestrictionType>> restrictedZones, EncodedPolyline polyline, double totalDistance, TimeSpan duration, double offroadDistance, double elevationGain, double elevationLoss, double maxGradientPercentage)
        {
            Segments = segments;
            Barriers = barriers;
            RestrictedZones = restrictedZones;
            Polyline = polyline;
            TotalDistanceMeters = totalDistance;
            Duration = duration;
            OffroadDistanceMeters = offroadDistance;
            ElevationGainMeters = elevationGain;
            ElevationLossMeters = elevationLoss;
            MaxGradientPercentage = maxGradientPercentage;
        }

        public static TripCandidate Create(IReadOnlyList<Segment> segments, IReadOnlyList<RoadBarrier> barriers, IReadOnlyList<Interval<RestrictionType>> restrictedZones, EncodedPolyline polyline, double totalDistance, TimeSpan duration, double elevationGain, double elevationLoss, double maxGradientPercentage = 0)
        {
            var offroadDistance = segments.Where(s => s.IsOffroad).Sum(s => s.DistanceMeters);
            Validate(totalDistance, duration, offroadDistance, elevationGain, elevationLoss);
            return new TripCandidate(segments, barriers, restrictedZones, polyline, totalDistance, duration, offroadDistance, elevationGain, elevationLoss, maxGradientPercentage);
        }
        protected static void Validate(double totalDistance, TimeSpan duration, double offroadDistance, double elevationGain, double elevationLoss)
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
