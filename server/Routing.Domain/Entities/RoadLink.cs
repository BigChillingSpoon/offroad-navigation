
namespace Offroad.Routing.Domain.Entities;

/// <summary>
/// A Value Object representing a connection from a Hookpoint to a Road and the next Hookpoint.
/// </summary>
/// <param name="RoadId">The ID of the connected road.</param>
/// <param name="ConnectedHookpointId">The ID of the hookpoint at the other end of the road.</param>
public record RoadLink(int RoadId, int ConnectedHookpointId);
