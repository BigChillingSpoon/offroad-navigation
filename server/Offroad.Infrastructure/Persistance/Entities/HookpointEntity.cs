using NetTopologySuite.Geometries;

namespace Routing.Infrastructure.Persistence.Entities;

public sealed class HookpointEntity
{
    public Point Geom { get; set; } = default!;
    public int PocetPrujezdu { get; set; }
    public int GradesMask { get; set; }
    public bool IsStrictlyInForest { get; set; }
}