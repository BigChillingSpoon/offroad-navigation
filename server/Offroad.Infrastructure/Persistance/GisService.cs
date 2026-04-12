using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Routing.Application.Contracts;
using Routing.Domain.Models; 
using Routing.Infrastructure.Data;

namespace Routing.Infrastructure.Persistance;

public class GisService : IGisService
{
    private readonly ApplicationDbContext _dbContext;

    public GisService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Polygon>> GetRestrictedZonesInAreaAsync(Geometry routeBoundingBox)
    {
        return await _dbContext.GeoZones
            .Where(z => z.Type == ZoneType.RestrictedArea)
            .Where(z => z.Geometry.Intersects(routeBoundingBox))
            .Select(z => z.Geometry)
            .ToListAsync();
    }
}