using Offroad.Core;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Models;
namespace Routing.Application.Planning.Finders
{
    public interface ILoopFinder
    {
        Task<Result<List<Trip>>> FindLoopsAsync(LoopIntent intent, UserRoutingProfile profile, CancellationToken ct);
    }
}
