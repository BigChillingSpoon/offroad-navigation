using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Routing.Domain.ValueObjects;
namespace Routing.Domain.Models
{
    public class TripPlan
    {
        public double TotalDistanceInMeters { get; private set; }
        public double OffroadDistanceMeters { get; private set; }
        public double DurationSeconds { get; private set; }
        public double ElevationGainMeters { get; private set; }
        public IReadOnlyList<Segment>? Segments { get; private set; }
        //for EFCore
        private TripPlan() { }

        private TripPlan(double distance, double offroadDistance, double duration, double elevationGain, IReadOnlyList<Segment> segments = null)
        {
            TotalDistanceInMeters = distance;
            DurationSeconds = duration;
            OffroadDistanceMeters = offroadDistance;
            Segments = segments ?? new List<Segment>();
            ElevationGainMeters = elevationGain;
        }

        public static TripPlan Create(double totalDistance, double offroadDistance, double duration, double elevationGain, IReadOnlyList<Segment> segments = null)
        {
            //todo add validation
            return new TripPlan(totalDistance, offroadDistance, duration, elevationGain, segments);
        }
    }
}
