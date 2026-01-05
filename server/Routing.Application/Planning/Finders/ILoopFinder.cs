using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
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
        Task<List<Trip>> FindLoopsAsync(LoopIntent intent, UserRoutingProfile profile, CancellationToken ct);
    }
}
