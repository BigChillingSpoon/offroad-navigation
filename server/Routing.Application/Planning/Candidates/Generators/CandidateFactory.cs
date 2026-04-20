using Routing.Application.Planning.Candidates.Builders;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Exceptions;
using Routing.Application.Planning.Encoding;
using Routing.Domain.ValueObjects;
using Routing.Domain.Utilities;
using Routing.Domain.Enums;
using Routing.Application.Planning.Extensions;

namespace Routing.Application.Planning.Candidates.Generators
{
    public sealed class TripCandidateFactory : ICandidateFactory
    {
        private readonly IRestrictedZoneBuilder _restrictedZoneBuilder;

        public TripCandidateFactory(IRestrictedZoneBuilder restrictedZoneBuilder)
        {
            _restrictedZoneBuilder = restrictedZoneBuilder;
        }

        public async Task<TripCandidate> CreateRouteCandidateAsync(ProviderRoute route)
        {
            var geometry = DecodeAndValidate(route.Polyline);

            return await BuildBaseCandidateInternalAsync(route, geometry, (segments, barriers, zones, maxGradient) =>
                TripCandidate.Create(
                    segments, barriers, zones, route.Polyline,
                    route.Distance, route.Duration, route.Ascend, route.Descend, maxGradient));
        }

        public async Task<LoopTripCandidate> CreateLoopCandidateAsync(ProviderRoute route, Coordinate userLocation)
        {
            var geometry = DecodeAndValidate(route.Polyline);

            //for loop we have to add different logic here
            return await BuildBaseCandidateInternalAsync(route, geometry, (segments, barriers, zones, maxGradient) =>
                LoopTripCandidate.CreateLoop(
                    segments, barriers, zones, route.Polyline,
                    route.Distance, route.Duration, route.Ascend, route.Descend, maxGradient,
                    default, default, default));
        }

        /// <summary>
        /// Shared internal method that does the heavy lifting:
        /// building segments, barriers, and analyzing restricted zones.
        /// </summary>
        private async Task<T> BuildBaseCandidateInternalAsync<T>(ProviderRoute route, IReadOnlyList<Coordinate> geometry, Func<IReadOnlyList<Segment>, IReadOnlyList<RoadBarrier>, IReadOnlyList<Interval<RestrictionType>>, double, T> factoryMethod)
        {
            var maxEdgeIndex = geometry.Count - 1;

            var segments = SegmentBuilder.Build(
                geometry,
                route.RoadClassIntervals.EnsureFullCoverage(maxEdgeIndex, RoadClassType.UNKNOWN),
                route.SurfaceIntervals.EnsureFullCoverage(maxEdgeIndex, SurfaceType.UNKNOWN),
                route.TrackTypeIntervals.EnsureFullCoverage(maxEdgeIndex, TrackType.UNKNOWN));

            var barriers = BarrierBuilder.Build(route.BarrierIntervals, geometry);

            var restrictedZones = await _restrictedZoneBuilder.BuildAsync(route.RoadAccessIntervals, geometry);

            var maxGradient = GeoCalculator.CalculateMaxGradientPercentage(geometry);

            return factoryMethod(segments, barriers, restrictedZones, maxGradient);
        }

        private IReadOnlyList<Coordinate> DecodeAndValidate(EncodedPolyline polyline)
        {
            try
            {
                var decoded = PolylineDecoder.Decode(polyline);
                if (decoded.Count < 2)
                    throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "Neplatná geometrie od providera.");
                return decoded;
            }
            catch (InvalidPolylineException ex)
            {
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, ex.Message);
            }
        }
    }
}