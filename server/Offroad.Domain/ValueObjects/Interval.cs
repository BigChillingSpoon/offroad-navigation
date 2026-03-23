namespace Routing.Domain.ValueObjects
{
    public sealed record Interval<T>(int FromIndex, int ToIndex, T Value);
}
