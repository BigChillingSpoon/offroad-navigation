using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Routing.Application.Contracts;
using Routing.Domain.Models; 
using Routing.Infrastructure.Data;

namespace Routing.Infrastructure.Persistance;

public class GisService : IGisService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public GisService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<Polygon>> GetRestrictedZonesInAreaAsync(Geometry routeBoundingBox)
    {
        // Fresh context per call: multiple candidate builders run concurrently under
        // Task.WhenAll, so each must own its DbContext — never share one instance.
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.GeoZones
            .Where(z => z.Type == ZoneType.RestrictedArea)
            .Where(z => z.Geometry.Intersects(routeBoundingBox))
            .Select(z => z.Geometry)
            .ToListAsync();
    }
}