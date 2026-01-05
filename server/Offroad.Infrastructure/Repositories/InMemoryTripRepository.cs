using Routing.Domain.Models;
using Routing.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Infrastructure.Repositories
{
    //for now I do not have an actual database, and I dont want to have some database that I have to modify 10x times during development
    public class InMemoryTripRepository : ITripRepository
    {
        private static readonly List<Trip> _trips = new();

        public Task AddAsync(Trip trip, CancellationToken ct)
        {
            _trips.Add(trip);
            return Task.CompletedTask;
        }

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var trip = _trips.FirstOrDefault(t => t.Id == id);
            return Task.FromResult(trip);
        }

        public Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var trip = _trips.FirstOrDefault(t => t.Id == id);
            if (trip != null)
            {
                _trips.Remove(trip);
            }
            return Task.CompletedTask;
        }

        // dont need to update list -> placeholder for interface
        public Task UpdateAsync(Trip trip, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Trip>> GetAllAsync(CancellationToken ct = default)
        {
            return Task.FromResult((IReadOnlyList<Trip>)_trips);
        }
    }
}
