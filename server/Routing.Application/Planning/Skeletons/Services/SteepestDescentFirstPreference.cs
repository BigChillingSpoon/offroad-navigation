using Routing.Application.Planning.Skeletons.Models;

namespace Routing.Application.Planning.Skeletons.Services;

/// <summary>
/// Default direction preference: keep whichever traversal takes the loop's steepest grade
/// downhill rather than uphill. This is a placeholder default - once rider/vehicle intent
/// (e.g. towing, engine-braking preference) is known, a different <see cref="ILoopDirectionPreference"/>
/// can be registered without touching the skeleton search itself.
/// </summary>
public sealed class SteepestDescentFirstPreference : ILoopDirectionPreference
{
    public List<SkeletonVector> ChooseDirection(List<SkeletonVector> chain, List<SkeletonVector> reversedChain)
    {
        return SteepestDescentGradePercentage(chain) >= SteepestDescentGradePercentage(reversedChain)
            ? chain
            : reversedChain;
    }

    private static double SteepestDescentGradePercentage(List<SkeletonVector> chain)
    {
        var steepest = 0.0;

        foreach (var vector in chain)
        {
            var fromElevation = vector.FromNode.Coordinate.Elevation;
            var toElevation = vector.ToNode.Coordinate.Elevation;

            if (fromElevation is null || toElevation is null || vector.EdgeLengthMeters <= 0)
                continue;

            var descentMeters = fromElevation.Value - toElevation.Value;
            if (descentMeters <= 0)
                continue; // flat or a climb in this direction

            var gradePercentage = descentMeters / vector.EdgeLengthMeters * 100.0;
            if (gradePercentage > steepest)
                steepest = gradePercentage;
        }

        return steepest;
    }
}
