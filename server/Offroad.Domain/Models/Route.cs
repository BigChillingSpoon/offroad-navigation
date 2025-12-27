namespace Routing.Domain.Models;

public sealed class Route
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    //todo add more properties like Waypoints, Distance, Difficulty, etc.
    private Route() { }

    public static Route Create(string name)
    {
        return new Route
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
    }
}
