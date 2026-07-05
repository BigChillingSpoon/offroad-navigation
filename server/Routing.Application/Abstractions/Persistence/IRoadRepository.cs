
using Offroad.Routing.Domain.Entities;

namespace Offroad.Routing.Application.Abstractions.Persistence;

public interface IRoadRepository
{
    Task<IReadOnlyList<Road>> GetRoadsByIdsAsync(IReadOnlyList<int> roadIds);
    Task<IReadOnlyList<Road>> GetAllInAreaAsync();
}
