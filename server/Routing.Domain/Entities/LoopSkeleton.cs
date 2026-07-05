
namespace Offroad.Routing.Domain.Entities;

/// <summary>
/// Represents a found valid loop or circuit. This is a Value Object in DDD terms.
/// It is immutable.
/// </summary>
public class LoopSkeleton
{
    /// <summary>
    /// The calculated score for the loop, used for ranking.
    /// </summary>
    public double TotalScore { get; }

    /// <summary>
    /// The total distance of the loop in meters.
    /// </summary>
    public double TotalDistanceMeters { get; }

    /// <summary>
    /// An ordered list of the roads that make up the loop.
    /// </summary>
    public IReadOnlyList<Road> Roads { get; }

    public LoopSkeleton(double totalScore, double totalDistanceMeters, IReadOnlyList<Road> roads)
    {
        TotalScore = totalScore;
        TotalDistanceMeters = totalDistanceMeters;
        Roads = roads ?? throw new ArgumentNullException(nameof(roads));
    }
}
