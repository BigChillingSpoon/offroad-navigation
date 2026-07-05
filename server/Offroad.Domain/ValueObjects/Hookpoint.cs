using Routing.Domain.ValueObjects;

namespace Routing.Domain.Models;

/// <summary>
/// Represents a pre-filtered, high-quality off-road intersection from the gis.offroad_hookpoints table.
/// </summary>
/// <param name="Id">The unique identifier of the hookpoint.</param>
/// <param name="Location">The geographic coordinate of the hookpoint.</param>
/// <param name="Passages">Represents 'connected_paths_count', indicating the popularity or number of traversals, used for sorting.</param>
/// <param name="GradesMask">A bitmask representing the available track difficulty grades (1-5) at this point.</param>
public record Hookpoint(
    Coordinate Location,
    int Passages,
    int GradesMask,
    bool IsStrictlyInForest);

