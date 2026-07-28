using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Routing.Application.Contracts;
using Routing.Infrastructure.Data;

namespace Routing.Infrastructure.Persistance;

public class GisService : IGisService
{
    // Matches import_rules.lua's zone_type numbering: 1 = Nature, 2 = Restricted.
    private const int RestrictedZoneType = 2;

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public GisService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Polygon>> GetRestrictedZonesInAreaAsync(Geometry routeBoundingBox)
    {
        // A fresh, short-lived context per call - this method is invoked concurrently (multiple
        // candidates built in parallel via Task.WhenAll), and DbContext is not thread-safe, so it
        // cannot be a single instance shared across the whole request like a normal scoped service.
        await using var dbContext = await _contextFactory.CreateDbContextAsync();

        var overlappingZones = await dbContext.GeometricZones
            .Where(z => z.ZoneType == RestrictedZoneType)
            .Where(z => z.Geometry.Intersects(routeBoundingBox))
            .Select(z => z.Geometry)
            .ToListAsync();

        // Each row's geometry is a MultiPolygon (possibly several disjoint park boundaries merged
        // during import) - flatten to individual polygons for the caller's point-in-polygon checks.
        return overlappingZones.SelectMany(mp => mp.Geometries.OfType<Polygon>()).ToList();
    }
}
