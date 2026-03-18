using Routing.Domain.ValueObjects;
using Routing.Infrastructure.GraphHopper.DTOs;

namespace Routing.Infrastructure.GraphHopper.Mappings
{
    internal static class GraphHopperAttributeIntervalMapper
    {
        public static IReadOnlyList<RoadClassInterval> MapRoadClass(IReadOnlyList<GraphHopperAttributeInterval<string>> source)
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

        public static IReadOnlyList<SurfaceInterval> MapSurface(IReadOnlyList<GraphHopperAttributeInterval<string>> source)
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

        public static IReadOnlyList<TrackTypeInterval> MapTrackType(IReadOnlyList<GraphHopperAttributeInterval<string>> source)
        {
            return source
               .Select(s => new TrackTypeInterval
               {
                   FromIndex = s.FromIndex,
                   ToIndex = s.ToIndex,
                   TrackType = GraphHopperTrackTypeMapper.Map(s.Value)
               })
               .ToList();
        }
        public static IReadOnlyList<BarrierInterval> MapBarriers(IReadOnlyList<GraphHopperAttributeInterval<string>> source)
        {
            return source
               .Where(s => s.Value != "none") //we can ignore íntervals without barriers
               .Select(s => new BarrierInterval
               {
                   FromIndex = s.FromIndex,
                   ToIndex = s.ToIndex,
                   BarrierType = GraphHopperBarrierMapper.Map(s.Value)
               })
               .ToList();
        }
    }
}
