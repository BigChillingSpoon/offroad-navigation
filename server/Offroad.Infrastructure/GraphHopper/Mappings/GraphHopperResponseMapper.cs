using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Exceptions;
using Routing.Infrastructure.GraphHopper.DTOs;
using Routing.Domain.Enums;
using Microsoft.Extensions.Options;
namespace Routing.Infrastructure.GraphHopper.Mappings
{
    public sealed class GraphHopperResponseMapper
    {
        private readonly IOptions<GraphHopperOptions> _options;
        public GraphHopperResponseMapper(IOptions<GraphHopperOptions> options)
        {
            _options = options;
        }

        public ProviderRoute ToProviderRoute(GraphHopperPath path)
        {
            if (path.Distance < 0)
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse,"Distance < 0");

            if (path.TimeMs < 0)
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "TimeMs < 0");

            if (string.IsNullOrEmpty(path.Points))
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "Missing geometry");

            if (path.Details is null)
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "Missing details section.");

            if(path.Details.SurfaceIntervals is null)
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "Missing surfaces details.");
            
            if (path.Details.RoadClassIntervals is null)
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "Missing road classes details.");
            
            var surfaceIntervals = GraphHopperAttributeIntervalMapper.MapSurface(path.Details.SurfaceIntervals);
            var roadClassIntervals = GraphHopperAttributeIntervalMapper.MapRoadClass(path.Details.RoadClassIntervals);

            return new ProviderRoute
            {
                Distance = path.Distance,
                Duration = TimeSpan.FromMilliseconds(path.TimeMs),
                Ascend = path.Ascend,
                Descend = path.Descend,
                SurfaceIntervals = surfaceIntervals,
                RoadClassIntervals = roadClassIntervals,
                Polyline = new EncodedPolyline
                {
                    Points = path.Points,
                    PolylineEncodedMultiplier = path.PointsEncodedMultiplier,
                    Dimension = _options.Value.Elevation == true ? PolylineDimension.ThreeDimensional : PolylineDimension.TwoDimensional
                }
            };
        }
    }
}
