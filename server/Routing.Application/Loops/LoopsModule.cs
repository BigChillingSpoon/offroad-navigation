using Offroad.Core;
using Routing.Application.Contracts;
using Routing.Application.Contracts.Models;
using Routing.Application.Contracts.Responses;
using Routing.Application.Loops.Commands;
using Routing.Application.Loops.Queries;

namespace Routing.Application.Loops
{
    internal sealed class LoopsModule : ILoopsModule
    {
        private readonly ILoopsQueries _queries;
        private readonly ILoopsCommands _commands;

        public LoopsModule(ILoopsQueries queries, ILoopsCommands commands)
        {
            _queries = queries;
            _commands = commands;
        }

        public Task<Result<TripResult>> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _queries.GetByIdAsync(id, ct);

        public Task<Result<IReadOnlyList<TripResult>>> GetAllAsync(CancellationToken ct = default)
            => _queries.GetAllAsync(ct);

        public Task<Result<Guid>> SaveAsync(SaveLoopRequest request, CancellationToken ct = default)
            => _commands.SaveAsyncCommand(request, ct);

        public Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
            => _commands.DeleteAsyncCommand(id, ct);

        public Task<Result<IReadOnlyList<TripResult>>> FindAsync(FindLoopsRequest request, CancellationToken ct = default)
            => _commands.FindAsyncCommand(request, ct);
    }
}
