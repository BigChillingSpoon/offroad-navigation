
namespace Routing.Domain.Entities;

/// <summary>
/// A Value Object representing a connection from a Node to an Edge and the next Node.
/// </summary>
/// <param name="EdgeId">The ID of the connected edge.</param>
/// <param name="ConnectedNodeId">The ID of the node at the other end of the edge.</param>
public record EdgeLink(long EdgeId, long ConnectedNodeId);
