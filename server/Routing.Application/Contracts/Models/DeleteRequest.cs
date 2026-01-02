namespace Routing.Application.Contracts.Models
{
    public sealed record DeleteRequest
    {
        public required Guid Id { get; init; }
        public Guid UserId { get; init; }//not required for now, in future we may need it for authorization
    }
}
