namespace Routing.Application.Planning.Intents;

/// <summary>
/// Acceptable deviation band around <see cref="LoopIntent.PreferredLengthKm"/>. Enforced twice:
/// once by the skeleton search while a candidate loop can still keep growing/shrinking, and again
/// by <see cref="Goals.LoopGoal"/> after GraphHopper has replaced the skeleton's estimated distance
/// with the real routed one.
/// </summary>
public static class LoopDistanceTolerance
{
    public const double MinFraction = 0.85;
    public const double MaxFraction = 1.15;
}
