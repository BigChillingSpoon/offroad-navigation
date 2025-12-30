namespace Routing.Application.Contracts.Models
{
    public sealed record SaveLoopRequest
    {
        public required string Name { get; init; }
    }
}
