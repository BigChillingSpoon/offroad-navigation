namespace Routing.Application.Contracts.Models
{
    public sealed record SaveRouteRequest
    {
        public required string Name { get; init; }
    }
}
