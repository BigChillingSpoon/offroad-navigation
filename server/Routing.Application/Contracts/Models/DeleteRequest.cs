namespace Routing.Application.Contracts.Models
{
    public sealed record DeleteRequest
    {
        public required Guid Id { get; init; }
    }
}
