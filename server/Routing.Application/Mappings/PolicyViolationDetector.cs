using Routing.Application.Contracts.Responses;
using Routing.Application.Planning.Intents;
using Routing.Domain.Enums;

namespace Routing.Application.Mappings;

public static class PolicyViolationDetector
{
    public static List<PolicyViolationType> Detect(IReadOnlyList<TripEvent> events, ITripIntent intent)
    {
        var violations = new List<PolicyViolationType>();

        if (!intent.AllowGates && events.Any(e => e.Type == TripEventType.Barrier))
            violations.Add(PolicyViolationType.Gates);

        if (!intent.AllowPrivateRoads && events.Any(e => e.Type == TripEventType.Restriction && e.SubType == nameof(RestrictionType.Private)))
            violations.Add(PolicyViolationType.PrivateRoads);

        return violations;
    }
}
