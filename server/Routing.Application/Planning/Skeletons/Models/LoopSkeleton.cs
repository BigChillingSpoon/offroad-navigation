using Routing.Domain.Models;
using Routing.Domain.ValueObjects;
using System.Collections.Generic;

namespace Routing.Application.Planning.Skeletons.Models;

/// <summary>
/// Represents a single valid output from the skeleton generation algorithm.
/// It contains an ordered list of waypoints that form the backbone of a potential loop.
/// </summary>
/// <param name="Waypoints">The ordered list of intermediate coordinates (junction nodes) forming the loop skeleton. Does NOT include the closing return to <paramref name="Entrance"/> - callers that need a closed path append Entrance themselves.</param>
/// <param name="Entrance">The entrance coordinate where the loop starts and ends.</param>
/// <param name="Geometry">The full traversed path (every edge's real geometry, oriented and concatenated, entrance-to-entrance), so the routing provider follows the exact roads the search chose instead of rerouting between sparse junctions.</param>
/// <param name="TraveledDistanceKm">The exact total distance in kilometers, summed from the real graph edges traversed by the skeleton (including the closing edge back to the entrance).</param>
public record LoopSkeleton(
    Coordinate Entrance,
    IReadOnlyList<Coordinate> Waypoints,
    IReadOnlyList<Coordinate> Geometry,
    float TraveledDistanceKm
    );

