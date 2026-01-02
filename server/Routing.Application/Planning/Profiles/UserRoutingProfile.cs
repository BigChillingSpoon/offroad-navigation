using Routing.Domain.Enums;

namespace Routing.Application.Planning.Profiles
{
    public sealed record UserRoutingProfile
    {
        public bool AllowPrivateRoads { get; init; }
        public bool AllowGates { get; init; }

        // In future 
        // public VehicleType VehicleType { get; init; }
        // public SkillLevel SkillLevel { get; init; }
    }
}
