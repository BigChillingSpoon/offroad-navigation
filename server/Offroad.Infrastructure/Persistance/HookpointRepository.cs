using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Routing.Application.Abstractions.Persistence;
using Routing.Domain.Models;
using Routing.Domain.ValueObjects;
using Routing.Infrastructure.Data;

namespace Routing.Infrastructure.Persistence.Repositories;

public sealed class HookpointRepository : IHookpointRepository
{
    private readonly ApplicationDbContext _dbContext;

    public HookpointRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Hookpoint>> GetHookpointsNearAsync(Routing.Domain.ValueObjects.Coordinate center, double radiusMeters, CancellationToken ct)
    {
        //this will be taken directly from entrance as geom
        var targetPoint = new Point(center.Longitude, center.Latitude) { SRID = 4326 };

        // 2. Čistý LINQ dotaz (EF Core to sám přeloží do ST_DWithin)
        // POZNÁMKA K METRŮM: Aby EF Core posílal metry a ne stupně, musíš mít entitu v EF 
        // namapovanou jako 'geography'. (Viz bod 2 níže).
        var entities = await _dbContext.Hookpoints
            .Where(h => h.Geom.Distance(targetPoint) <= radiusMeters)
            .ToListAsync(ct);

        // 3. Namapování DB entit na tvůj čistý Doménový model
        return entities.Select(e => new Hookpoint(
            new Routing.Domain.ValueObjects.Coordinate(e.Geom.Y, e.Geom.X),
            e.PocetPrujezdu,
            e.GradesMask,
            e.IsStrictlyInForest
        )).ToList();
    }
}