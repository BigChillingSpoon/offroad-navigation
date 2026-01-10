using Offroad.Core;
using Routing.Application.Contracts.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Routes.Queries
{
    public interface IRoutesQueries
    {
        Task<Result<TripResult>> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Result<IReadOnlyList<TripResult>>> GetAllAsync(CancellationToken ct);
    }
}
