using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Application.Contracts.Responses;

namespace Routing.Application.Loops.Commands
{
    public interface ILoopsCommands
    {
        Task<Result<IReadOnlyList<TripResult>>> FindAsync(FindLoopsRequest request, CancellationToken ct);
        Task<Result<Guid>> SaveAsync(SaveLoopRequest request, CancellationToken ct);
        Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct);
    }
}
