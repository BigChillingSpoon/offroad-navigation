using Offroad.Core;
using Routing.Application.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Routes.Queries
{
    public interface IRouteQueries
    {
        Task<Result<TripResult>> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Result<IReadOnlyList<TripResult>>> GetAllAsync(CancellationToken ct);
    }
}
