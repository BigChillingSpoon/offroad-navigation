namespace Routing.Application.Contracts.Models
{
    public sealed record RouteInfo
    (
        Guid Id,
        string Name,
        bool IsLoop,
        double TotalDistanceKm,
        double OffroadDistanceKm,
        TimeSpan EstimatedTime
    );
}
