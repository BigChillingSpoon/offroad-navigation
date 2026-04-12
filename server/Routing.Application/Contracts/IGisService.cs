using NetTopologySuite.Geometries;

namespace Routing.Application.Contracts;

public interface IGisService
{
    Task<List<Polygon>> GetRestrictedZonesInAreaAsync(Geometry routeBoundingBox);
}