using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;
using System.Text.Json.Serialization;

namespace Routing.Application.Contracts.Responses
{
    [JsonDerivedType(typeof(PointEvent))]
    [JsonDerivedType(typeof(IntervalEvent))]
    public abstract record TripEvent
    {
        public string Type { get; init; } // "barrier", "private_road", atd.
        public string SubType { get; init; } // "gate", "bollard", "toll_gantry", "ferry", atd.

    }

    public sealed record PointEvent : TripEvent
    {
        public int PointIndex { get; init; }
        public Coordinate Coordinate { get; init; }
    }

    public sealed record IntervalEvent : TripEvent
    {
        public int FromIndex { get; init; }
        public int ToIndex { get; init; }
    }
}
