using Routing.Domain.ValueObjects;

namespace Routing.Application.Contracts.Models
{
    public sealed record FindLoopsRequest(
        Coordinate Start,
        LoopPreferences Preferences
    );
}
