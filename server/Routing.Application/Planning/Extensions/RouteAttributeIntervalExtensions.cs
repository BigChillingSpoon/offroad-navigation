using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Extensions
{
    internal static class IntervalExtensions
    {
        public static IReadOnlyList<Interval<T>> EnsureFullCoverage<T>(this IReadOnlyList<Interval<T>> source, int maxEdgeIndex, T defaultValue)
        {
            var ordered = source.OrderBy(s => s.FromIndex).ToList();
            var result = new List<Interval<T>>();
            int currentIndex = 0;

            foreach (var interval in ordered)
            {
                if (interval.FromIndex > currentIndex)
                    result.Add(new Interval<T>(currentIndex, interval.FromIndex, defaultValue));

                result.Add(interval);
                currentIndex = interval.ToIndex;
            }

            if (currentIndex < maxEdgeIndex)
                result.Add(new Interval<T>(currentIndex, maxEdgeIndex, defaultValue));

            return result;
        }
    }
}
