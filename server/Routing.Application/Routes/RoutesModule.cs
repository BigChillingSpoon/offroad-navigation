using Offroad.Core;
using Routing.Application.Contracts;
using Routing.Application.Contracts.Models;
using Routing.Application.Routes.Commands;
using Routing.Application.Routes.Queries;

namespace Routing.Application.Routes
{
    internal sealed class RoutesModule : IRoutesModule
    {
        private readonly IRoutesQueries _queries;
        private readonly IRoutesCommands _commands;

        public RoutesModule(IRoutesQueries queries, IRoutesCommands commands)
        {
            _queries = queries;
            _commands = commands;
        }

        public Task<Result<TripResult>> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _queries.GetByIdAsync(id, ct);

        public Task<Result<IReadOnlyList<TripResult>>> GetAllAsync(CancellationToken ct = default)
            => _queries.GetAllAsync(ct);

        public Task<Result<Guid>> SaveAsync(SaveRouteRequest request, CancellationToken ct = default)
            => _commands.SaveAsyncCommand(request, ct);

        public Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
            => _commands.DeleteAsyncCommand(id, ct);

        public Task<Result<TripResult>> PlanAsync(PlanRouteRequest request, CancellationToken ct = default)
            => _commands.PlanAsyncCommand(request, ct);
    }
}
