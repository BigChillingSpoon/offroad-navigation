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

    public async Task<IReadOnlyList<Node>> GetNodesNearAsync(Coordinate center, double radiusMeters, int maxNodes, CancellationToken ct)
    {
        // A fresh, short-lived context per call - this is invoked concurrently (once multiple
        // entrances are searched in parallel via Task.WhenAll), and DbContext is not thread-safe.
        await using var dbContext = await _contextFactory.CreateDbContextAsync(ct);

        var radiusDegrees = radiusMeters / MetersPerDegree;

        // gis.nodes.geom is a plain geometry(Point, 4326) column, not geography, so ST_Distance/.Distance()
        // would return degrees rather than meters. The "&&"/ST_Expand check is an index-accelerated
        // bounding-box pre-filter against the GiST index on geom; ST_DWithin on the geography cast then
        // makes the exact, accurate meter-based decision. The KNN "<->" ORDER BY + LIMIT keeps only the
        // nearest maxNodes: a round loop stays near its start, so a huge fetch radius (big loops) would
        // otherwise pull thousands of far-away nodes that only bloat the skeleton search.
        var entities = await dbContext.Nodes
            .FromSqlInterpolated($@"
                SELECT node_id, geom, is_entry_point, altitude
                FROM gis.nodes
                WHERE geom && ST_Expand(ST_SetSRID(ST_MakePoint({center.Longitude}, {center.Latitude}), 4326), {radiusDegrees})
                  AND ST_DWithin(
                      geom::geography,
                      ST_SetSRID(ST_MakePoint({center.Longitude}, {center.Latitude}), 4326)::geography,
                      {radiusMeters}
                  )
                ORDER BY geom <-> ST_SetSRID(ST_MakePoint({center.Longitude}, {center.Latitude}), 4326)
                LIMIT {maxNodes}")
            .ToListAsync(ct);

        return entities.Select(e => new Node(
            e.Id,
            new Coordinate(e.Geom.Y, e.Geom.X, e.Altitude),
            e.IsEntryPoint
        )).ToList();
    }

    public async Task<IReadOnlyList<ArenaEntrance>> GetCandidateArenaEntrancesAsync(
        Coordinate center,
        double driveRadiusMeters,
        double loopReachMeters,
        double minOffroadLengthMeters,
        double rankingLengthBlend,
        int limit,
        CancellationToken ct)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(ct);

        // Degrees for the index-accelerated bounding-box pre-filters (see GetNodesNearAsync above for
        // why: the "&&"/ST_Expand check rides the GiST index, then ST_DWithin on the geography cast
        // makes the exact meter-based decision the geometry column alone can't).
        var driveDegrees = driveRadiusMeters / MetersPerDegree;
        var reachDegrees = loopReachMeters / MetersPerDegree;

        // Candidate entrances = every is_entry_point node in drive range, plus the user's own start
        // (so the caller can pick a no-drive loop when the user is already inside offroad terrain).
        // Each candidate is scored by the summed offroad length of gis.heatmap density-grid cells
        // within the loop reach radius - a handful of pre-aggregated rows instead of raw edge scans.
        var rows = await dbContext.Database
            .SqlQuery<ArenaEntranceRow>($@"
                WITH origin AS (
                    SELECT ST_SetSRID(ST_MakePoint({center.Longitude}, {center.Latitude}), 4326) AS g
                ),
                candidates AS (
                    SELECT n.geom AS g
                    FROM gis.nodes n, origin o
                    WHERE n.is_entry_point
                      AND n.geom && ST_Expand(o.g, {driveDegrees})
                      AND ST_DWithin(n.geom::geography, o.g::geography, {driveRadiusMeters})
                    UNION ALL
                    SELECT o.g FROM origin o
                ),
                reach AS (
                    -- One pass over the density grid per candidate: total offroad length and edge count
                    -- within the loop reach radius.
                    SELECT
                        ST_X(c.g) AS lon,
                        ST_Y(c.g) AS lat,
                        COALESCE(SUM(h.offroad_len_m), 0) AS off_len,
                        COALESCE(SUM(h.edge_count), 0)   AS edge_cnt
                    FROM candidates c
                    LEFT JOIN gis.heatmap h
                      ON h.cell_geom && ST_Expand(c.g, {reachDegrees})
                     AND ST_DWithin(h.cell_geom::geography, c.g::geography, {loopReachMeters})
                    GROUP BY c.g
                ),
                scored AS (
                    SELECT
                        lon AS ""Longitude"",
                        lat AS ""Latitude"",
                        off_len AS ""OffroadLengthMeters"",
                        -- Length-aware ranking: t=0 -> edge density, t=0.5 -> length, t=1 -> length favouring long edges.
                        power(GREATEST(off_len, 1), 2 * {rankingLengthBlend})
                          * power(GREATEST(edge_cnt, 1), 1 - 2 * {rankingLengthBlend}) AS ""ArenaScore""
                    FROM reach
                )
                SELECT ""Longitude"", ""Latitude"", ""OffroadLengthMeters"", ""ArenaScore""
                FROM scored
                WHERE ""OffroadLengthMeters"" >= {minOffroadLengthMeters}
                ORDER BY ""ArenaScore"" DESC
                LIMIT {limit}")
            .ToListAsync(ct);

        return rows
            .Select(r => new ArenaEntrance(new Coordinate(r.Latitude, r.Longitude), r.OffroadLengthMeters, r.ArenaScore))
            .ToList();
    }

    // Projection target for the arena-scoring query above. Column names in the SQL are quoted to match
    // these property names exactly (Npgsql treats quoted identifiers as case-sensitive).
    private sealed record ArenaEntranceRow(double Longitude, double Latitude, double OffroadLengthMeters, double ArenaScore);
}
