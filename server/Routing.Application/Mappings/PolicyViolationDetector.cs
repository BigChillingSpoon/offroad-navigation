using Routing.Application.Contracts.Responses;
using Routing.Application.Planning.Intents;
using Routing.Domain.Enums;
using Routing.Domain.Utilities;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Mappings;

public static class PolicyViolationDetector
{
    public static List<PolicyViolationType> Detect(IReadOnlyList<TripEvent> events, ITripIntent intent, IReadOnlyList<Coordinate> polyline)
    {
        var violations = new List<PolicyViolationType>();
        const double DestinationTolerance = 50.0; //meters

        // we do filter barriers only if user disables them
        if (!intent.AllowGates)
        {
            bool hasRealViolation = events
                .Where(e => e.Type == TripEventType.Barrier)
                .Any(barrier => !IsWithinLastMile(barrier, polyline, DestinationTolerance));

            if (hasRealViolation) violations.Add(PolicyViolationType.Gates);
        }

        // same logic as for barriers
        if (!intent.AllowPrivateRoads)
        {
            bool hasRealViolation = events
                .Where(e => e.Type == TripEventType.Restriction)
                .Any(barrier => !IsWithinLastMile(barrier, polyline, DestinationTolerance));

            if (hasRealViolation) violations.Add(PolicyViolationType.RestrictedArea);
        }

        return violations;
    }

    private static bool IsWithinLastMile(TripEvent @event, IReadOnlyList<Coordinate> decodedPolyline, double tolerance)
    {
        int checkFromIndex = @event switch
        {
            PointEvent pt => pt.PointIndex,
            IntervalEvent iv => iv.ToIndex,
            _ => throw new NotSupportedException($"Unknown event type: {@event.GetType().Name}")
        };

        double distanceSum = 0;

        for (int i = checkFromIndex; i < decodedPolyline.Count - 1; i++)
        {
            distanceSum += GeoCalculator.CalculateDistance(decodedPolyline[i], decodedPolyline[i + 1]);

            if (distanceSum > tolerance)
            {
                return false;
            }
        }

        return true;
    }
}
