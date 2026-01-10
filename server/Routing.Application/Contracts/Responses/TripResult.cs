using Routing.Domain.Enums;

namespace Routing.Application.Contracts.Responses
{
    public sealed record TripResult
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public TripType Type { get; init; }
        public TripDetails Details { get; init; }
        public TripMetrics Metrics { get; init; }
    }
}
