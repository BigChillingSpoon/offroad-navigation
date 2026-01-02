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
        //public IReadOnlyList<Segment> Segments { get; private set; }

        //for EFCore
        private TripPlan() { }

        private TripPlan(double distance, double offroadDistance, double duration/*, List<Segment> segments*/)
        {
            TotalDistanceInMeters = distance;
            DurationSeconds = duration;
            OffroadDistanceMeters = offroadDistance;
            //Segments = segments;
        }

        public static TripPlan Create(double totalDistance, double offroadDistance, double duration /*,List<Segment> segments*/)
        {
            //todo add validation
            return new TripPlan(totalDistance, offroadDistance, duration/*, segments*/);
        }
    }
}
