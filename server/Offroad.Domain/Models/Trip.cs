using Routing.Domain.Enums;
using Offroad.Core.Abstraction;
using Routing.Domain.Exceptions;

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
            if(plan is null)
                throw new DomainException("Plan cannot be null while creating TripPlan.");

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
            if (Name == newName)
                return;//leave without updates
            Name = newName;
            UpdatedOnUtc = DateTime.UtcNow;
        }
    }
}
