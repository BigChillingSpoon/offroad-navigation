using Microsoft.EntityFrameworkCore;
using Routing.Application.Abstractions.Persistence;
using Routing.Domain.Entities;
using Routing.Domain.ValueObjects;
using Routing.Infrastructure.Data;
using Routing.Infrastructure.Persistence.Entities;

namespace Routing.Infrastructure.Persistence.Repositories;

public sealed class EdgeRepository : IEdgeRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public EdgeRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Edge>> GetEdgesByIdsAsync(IReadOnlyList<long> edgeIds)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();

        var entities = await dbContext.Edges
            .Where(e => edgeIds.Contains(e.Id))
            .ToListAsync();

        return entities.Select(ToEdge).ToList();
    }

    public async Task<IReadOnlyList<Edge>> GetAllEdgesInAreaAsync()
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();

        var entities = await dbContext.Edges.ToListAsync();

        return entities.Select(ToEdge).ToList();
    }

    public async Task<IReadOnlyList<Edge>> GetEdgesConnectingAsync(IReadOnlyList<long> nodeIds, CancellationToken ct)
    {
        // A fresh, short-lived context per call - this is invoked concurrently (once multiple
        // entrances are searched in parallel via Task.WhenAll), and DbContext is not thread-safe.
        await using var dbContext = await _contextFactory.CreateDbContextAsync(ct);

        var entities = await dbContext.Edges
            .Where(e => nodeIds.Contains(e.SourceNodeId) && nodeIds.Contains(e.TargetNodeId))
            .ToListAsync(ct);

        return entities.Select(ToEdge).ToList();
    }

    private static Edge ToEdge(EdgeEntity e) => new(
        e.Id,
        e.SourceNodeId,
        e.TargetNodeId,
        e.LengthMeters,
        e.HasBarrier,
        e.HasNoEntry,
        e.IsRestricted,
        e.ElevationGainMeters,
        e.IsOffroad,
        e.Geom?.Coordinates.Select(c => new Coordinate(c.Y, c.X)).ToList()
    );
}
