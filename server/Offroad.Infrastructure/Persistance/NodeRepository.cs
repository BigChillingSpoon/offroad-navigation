using Microsoft.EntityFrameworkCore;
using Routing.Application.Abstractions.Persistence;
using Routing.Domain.Entities;
using Routing.Domain.ValueObjects;
using Routing.Infrastructure.Data;

namespace Routing.Infrastructure.Persistence.Repositories;

public sealed class NodeRepository : INodeRepository
{
    // Rough conversion used only for the index-accelerated bounding-box pre-filter below;
    // the exact geodesic decision is still made by ST_DWithin on the geography cast.
    private const double MetersPerDegree = 111320.0;

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public NodeRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Node>> GetNodesNearAsync(Coordinate center, double radiusMeters, CancellationToken ct)
    {
        // A fresh, short-lived context per call - this is invoked concurrently (once multiple
        // entrances are searched in parallel via Task.WhenAll), and DbContext is not thread-safe.
        await using var dbContext = await _contextFactory.CreateDbContextAsync(ct);

        var radiusDegrees = radiusMeters / MetersPerDegree;

        // gis.nodes.geom is a plain geometry(Point, 4326) column, not geography, so ST_Distance/.Distance()
        // would return degrees rather than meters. The "&&"/ST_Expand check is an index-accelerated
        // bounding-box pre-filter against the GiST index on geom; ST_DWithin on the geography cast then
        // makes the exact, accurate meter-based decision.
        var entities = await dbContext.Nodes
            .FromSqlInterpolated($@"
                SELECT node_id, geom, is_entry_point, altitude
                FROM gis.nodes
                WHERE geom && ST_Expand(ST_SetSRID(ST_MakePoint({center.Longitude}, {center.Latitude}), 4326), {radiusDegrees})
                  AND ST_DWithin(
                      geom::geography,
                      ST_SetSRID(ST_MakePoint({center.Longitude}, {center.Latitude}), 4326)::geography,
                      {radiusMeters}
                  )")
            .ToListAsync(ct);

        return entities.Select(e => new Node(
            e.Id,
            new Coordinate(e.Geom.Y, e.Geom.X, e.Altitude),
            e.IsEntryPoint
        )).ToList();
    }
}
