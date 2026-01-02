using Routing.Domain.Enums;
using Offroad.Core.Abstraction;

namespace Routing.Domain.Models
{
    public class Trip : AggregateRoot<Guid>
    {
        public string Name { get; private set; }
        public TripType Type { get; private set; }
        public DateTime CreatedOnUtc { get; private set; }
        public DateTime? UpdatedOnUtc { get; private set; }
        public TripPlan Plan { get; private set; }

        private Trip() { }

        public static Trip Create(string name, TripType type, TripPlan plan)
        {
            return new Trip
            {
                Id = Guid.NewGuid(),
                Name = name,
                Type = type,
                Plan = plan,
                CreatedOnUtc = DateTime.UtcNow
            };
        }

        public void UpdateName(string newName)
        {
            // Validace...
            Name = newName;
            UpdatedOnUtc = DateTime.UtcNow;
        }
    }
}
