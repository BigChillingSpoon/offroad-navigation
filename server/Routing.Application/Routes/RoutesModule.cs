using Offroad.Core;
using Routing.Application.Contracts;
using Routing.Application.Contracts.Models;
using Routing.Application.Routes.Commands;
using Routing.Application.Routes.Queries;

namespace Routing.Application.Routes
{
    internal sealed class RoutesModule : IRoutesModule
    {
        private readonly RoutesQueries _queries;
        private readonly RoutesCommands _commands;

        public RoutesModule(RoutesQueries queries, RoutesCommands commands)
        {
            _queries = queries;
            _commands = commands;
        }

        public Task<Result<RouteInfo>> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _queries.GetByIdAsync(id, ct);

        public Task<Result<IReadOnlyList<RouteInfo>>> GetAllAsync(CancellationToken ct = default)
            => _queries.GetAllAsync(ct);

        public Task<Result<Guid>> SaveAsync(SaveRouteRequest request, CancellationToken ct = default)
            => _commands.SaveAsync(request, ct);

        public Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
            => _commands.DeleteAsync(id, ct);

        public Task<Result<RouteInfo>> PlanAsync(PlanRouteRequest request, CancellationToken ct = default)
            => _commands.PlanAsync(request, ct);
    }
}
