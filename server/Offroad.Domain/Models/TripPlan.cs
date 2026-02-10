using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Routing.Domain.Exceptions;
using Routing.Domain.ValueObjects;
namespace Routing.Domain.Models
{
    public class TripPlan
    {
        public double TotalDistanceInMeters { get; private set; }
        public double OffroadDistanceMeters { get; private set; }
        public TimeSpan Duration { get; private set; }
        public double ElevationGainMeters { get; private set; }
        public IReadOnlyList<Segment> Segments { get; private set; }
        //for EFCore
        private TripPlan() { }

        private TripPlan(double distance, double offroadDistance, TimeSpan duration, double elevationGain, IReadOnlyList<Segment> segments = null)
        {
            TotalDistanceInMeters = distance;
            Duration = duration;
            OffroadDistanceMeters = offroadDistance;
            Segments = segments ?? new List<Segment>();
            ElevationGainMeters = elevationGain;
        }

        public static TripPlan Create(double totalDistance, double offroadDistance, TimeSpan duration, double elevationGain, IReadOnlyList<Segment> segments = null)
        {
            Validate(totalDistance, offroadDistance, duration, elevationGain);
            return new TripPlan(totalDistance, offroadDistance, duration, elevationGain, segments);
        }
        private static void Validate(double totalDistance, double offroadDistance, TimeSpan duration, double elevationGain)
        {
            if(totalDistance < 0)
                throw new DomainException("Total distance cannot be negative.");
            if (offroadDistance < 0)
                throw new DomainException("Offroad distance cannot be negative.");
        }
    }
}
