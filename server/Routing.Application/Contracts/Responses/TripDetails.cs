using Routing.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public sealed record TripDetails
{
    public required string Polyline { get; init; }
    public required Coordinate Start { get; init; }
    public required Coordinate End { get; init; }
    //todo add bounds + turn instructions
}

