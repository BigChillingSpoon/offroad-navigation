using Routing.Domain.ValueObjects;
using Routing.Infrastructure.GraphHopper.DTOs;

namespace Routing.Infrastructure.GraphHopper.Mappings
{
    internal static class GraphHopperAttributeIntervalMapper
    {
        public static IReadOnlyList<Interval<TEnum>> Map<TEnum>(
            IReadOnlyList<GraphHopperAttributeInterval<string>> source,
            Func<string, TEnum> valueMapper)
        {
            return source
                .Select(s => new Interval<TEnum>(s.FromIndex, s.ToIndex, valueMapper(s.Value)))
                .ToList();
        }

        public static IReadOnlyList<Interval<TEnum>> MapFiltered<TEnum>(
            IReadOnlyList<GraphHopperAttributeInterval<string>> source,
            Func<string, TEnum> valueMapper,
            Func<string, bool> filter)
        {
            return source
                .Where(s => filter(s.Value))
                .Select(s => new Interval<TEnum>(s.FromIndex, s.ToIndex, valueMapper(s.Value)))
                .ToList();
        }
    }
}
