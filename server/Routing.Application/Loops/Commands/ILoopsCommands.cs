using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Application.Contracts.Responses;

namespace Routing.Application.Loops.Commands
{
    public interface ILoopsCommands
    {
        Task<Result<IReadOnlyList<TripResult>>> FindAsyncCommand(FindLoopsRequest request, CancellationToken ct);
        Task<Result<Guid>> SaveAsyncCommand(SaveLoopRequest request, CancellationToken ct);
        Task<Result<bool>> DeleteAsyncCommand(Guid id, CancellationToken ct);
    }
}
