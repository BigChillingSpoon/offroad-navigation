
using Offroad.Routing.Domain.Common;

namespace Offroad.Routing.Domain.Entities;

/// <summary>
/// Represents a road segment connecting two hookpoints. This is an Entity in DDD terms.
/// </summary>
public class Road : BaseEntity
{
    /// <summary>
    /// The quality or type of the road, typically on a scale (e.g., 1-5).
    /// </summary>
    public int Grade { get; private set; }

    /// <summary>
    /// The ID of the hookpoint where the road segment starts.
    /// </summary>
    public int SourceHookpointId { get; private set; }

    /// <summary>
    /// The ID of the hookpoint where the road segment ends.
    /// </summary>
    public int TargetHookpointId { get; private set; }

    /// <summary>
    /// The gradient of the road when traversing from Source to Target. A negative value indicates the opposite gradient.
    /// </summary>
    public double Gradient { get; private set; }

    /// <summary>
    /// The length of the road in meters.
    /// </summary>
    public double LengthMeters { get; private set; }

    // Private constructor for EF Core
    private Road() { }

    public Road(int id, int grade, int sourceHookpointId, int targetHookpointId, double gradient, double lengthMeters)
    {
        Id = id;
        Grade = grade;
        SourceHookpointId = sourceHookpointId;
        TargetHookpointId = targetHookpointId;
        Gradient = gradient;
        LengthMeters = lengthMeters;
    }
}
