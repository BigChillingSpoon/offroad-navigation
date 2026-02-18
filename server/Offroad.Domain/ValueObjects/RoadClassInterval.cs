using Routing.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Domain.ValueObjects
{
    public sealed record RoadClassInterval : RouteAttributeInterval
    {
        public required RoadClassType RoadClass { get; init; }
    }

}
