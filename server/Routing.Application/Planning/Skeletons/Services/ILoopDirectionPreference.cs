using Routing.Application.Planning.Skeletons.Models;

namespace Routing.Application.Planning.Skeletons.Services;

/// <summary>
/// Chooses which traversal direction to keep when the skeleton search finds the same physical
/// loop twice - once forward, once backward (same nodes/edges, opposite order). Kept as a
/// swappable strategy because the "right" direction is a rider/vehicle preference, not a fixed
/// rule - e.g. a future vehicle profile might prefer climbing a steep grade over descending it.
/// </summary>
public interface ILoopDirectionPreference
{
    /// <param name="chain">One traversal of the loop.</param>
    /// <param name="reversedChain">The same loop traversed in the opposite direction.</param>
    /// <returns>Whichever of the two chains should be kept.</returns>
    List<SkeletonVector> ChooseDirection(List<SkeletonVector> chain, List<SkeletonVector> reversedChain);
}
