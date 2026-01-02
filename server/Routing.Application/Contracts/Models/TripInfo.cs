namespace Routing.Application.Contracts.Models
{
    public sealed record TripInfo
    (
        Guid Id,
        string Name,
        bool IsLoop,
        double TotalDistanceKm,
        double OffroadDistanceKm,
        TimeSpan EstimatedTime
    );
}
