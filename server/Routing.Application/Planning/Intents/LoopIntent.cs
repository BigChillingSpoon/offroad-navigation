using Routing.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Intents
{
    public sealed record LoopIntent : IRoutingIntent
    {
        public required Coordinate Start { get; init; }
        public double PreferredLengthKm { get; init; }
        public double MaxDriveDistanceKm { get; init; }
    }
}
