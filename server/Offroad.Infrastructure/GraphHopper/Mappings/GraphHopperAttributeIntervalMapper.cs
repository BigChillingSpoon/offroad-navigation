using Routing.Domain.ValueObjects;
using Routing.Infrastructure.GraphHopper.DTOs;

namespace Routing.Infrastructure.GraphHopper.Mappings
{
    internal static class GraphHopperAttributeIntervalMapper
    {
        public static IReadOnlyList<RoadClassInterval> MapRoadClass(IReadOnlyList<GraphHopperAttributeInterval> source)
        {
            return source
               .Select(s => new RoadClassInterval
               {
                   FromIndex = s.FromIndex,
                   ToIndex = s.ToIndex,
                   RoadClass = GraphHopperRoadClassMapper.Map(s.Value)
               })
               .ToList();
        }

        public static IReadOnlyList<SurfaceInterval> MapSurface(IReadOnlyList<GraphHopperAttributeInterval> source)
        {
            return source
               .Select(s => new SurfaceInterval
               {
                   FromIndex = s.FromIndex,
                   ToIndex = s.ToIndex,
                   Surface = GraphHopperSurfaceMapper.Map(s.Value)
               })
               .ToList();
        }
    }
}
