using Offroad.Core;
using Routing.Application.Contracts;
using Routing.Application.Contracts.Models;
using Routing.Application.Loops.Commands;
using Routing.Application.Loops.Queries;

namespace Routing.Application.Loops
{
    internal sealed class LoopsModule : ILoopsModule
    {
        private readonly LoopsQueries _queries;
        private readonly LoopsCommands _commands;

        public LoopsModule(LoopsQueries queries, LoopsCommands commands)
        {
            _queries = queries;
            _commands = commands;
        }

        public Task<Result<TripInfo>> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _queries.GetByIdAsync(id, ct);

        public Task<Result<IReadOnlyList<TripInfo>>> GetAllAsync(CancellationToken ct = default)
            => _queries.GetAllAsync(ct);

        public Task<Result<Guid>> SaveAsync(SaveLoopRequest request, CancellationToken ct = default)
            => _commands.SaveAsync(request, ct);

        public Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
            => _commands.DeleteAsync(id, ct);

        public Task<Result<IReadOnlyList<TripInfo>>> FindAsync(FindLoopsRequest request, CancellationToken ct = default)
            => _commands.FindAsync(request, ct);
    }
}
