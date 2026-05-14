using Routing.Domain.Models;
using Routing.Domain.ValueObjects;
using System.Collections.Generic;

namespace Routing.Application.Planning.Skeletons.Models;

/// <summary>
/// Represents a single valid output from the skeleton generation algorithm.
/// It contains an ordered list of waypoints that form the backbone of a potential loop.
/// </summary>
/// <param name="Waypoints">The ordered list of coordinates forming the loop skeleton.</param>
/// <param name="Entrance">The entrance coordinate where the loop starts and ends.</param>
/// <param name="EucledianDistanceKm">The straight-line distance in kilometers from the entrance to the furthest waypoint, used for filtering and timeout estimation.</param>
public record LoopSkeleton(
    Coordinate Entrance,
    IReadOnlyList<Coordinate> Waypoints,
    float EucledianDistanceKm
    );

