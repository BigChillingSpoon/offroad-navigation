using Routing.Domain.Models;
using Routing.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Finders
{
    public interface ILoopFinder
    {
        // Vrací seznam, protože LoopFinder generuje více variant
        Task<List<Trip>> FindLoopsAsync(Coordinate start, double distanceKm, CancellationToken ct);
    }
}
