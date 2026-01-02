using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Intents
{
    public interface ITripIntent
    {
        Coordinate Start { get; }
    }
}
