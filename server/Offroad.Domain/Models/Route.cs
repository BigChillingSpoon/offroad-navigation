namespace Routing.Domain.Models
{
    public sealed class Route
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public bool IsLoop { get; private set; }
        public bool HasPrivateRoads { get; private set; }
        public bool HasGates { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        //todo add more properties like Waypoints, Distance, Difficulty, etc.
        private Route() { }

        public static Route Create( string name, bool isLoop = false, bool hasPrivateRoads = false, bool hasGates = false)
        {
            return new Route
            {
                Id = Guid.NewGuid(),
                Name = name,
                IsLoop = isLoop,
                HasPrivateRoads = hasPrivateRoads,
                HasGates = hasGates,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}


