using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Intents
{
    public sealed record RouteIntent : IRoutingIntent
    {
        public required Coordinate Start { get; init; }
        public required Coordinate End { get; init; }
        public RouteBalance Balance { get; init; } 
    }
}
