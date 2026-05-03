using System.Collections.Generic;
using Routing.Application.Planning.Profiles;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Ports.DTOs;

/// <summary>
/// A focused Data Transfer Object for making a skeleton-based route request to the IRoutingProvider.
/// It contains only the information the provider needs for this specific task.
/// </summary>
/// <param name="Waypoints">The ordered list of coordinates that form the skeleton's path.</param>
public sealed record ProviderSkeletonRequest(
    IReadOnlyList<Coordinate> Waypoints
    //maybe add something here, if not we will just pass the list of coordinates and delete this class
);
