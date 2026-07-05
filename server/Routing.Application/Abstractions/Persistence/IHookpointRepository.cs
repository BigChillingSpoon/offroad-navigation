
using Offroad.Routing.Domain.Entities;

namespace Offroad.Routing.Application.Abstractions.Persistence;

public interface IHookpointRepository
{
    Task<Hookpoint?> GetByIdAsync(int id);
    Task<IReadOnlyList<Hookpoint>> GetAllInAreaAsync();
}
