using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Index.Strtree;
using Routing.Application.Contracts;
using Routing.Domain.Enums;
using Routing.Domain.Exceptions;
using Routing.Domain.ValueObjects;
using Coordinate = Routing.Domain.ValueObjects.Coordinate;

namespace Routing.Application.Planning.Candidates.Builders
{
    // POZOR: Budeš muset upravit i IRestrictedZoneBuilder interface, aby vracel Task!
    public class RestrictedZoneBuilder : IRestrictedZoneBuilder
    {
        private readonly GeometryFactory _geometryFactory;
        private readonly IGisService _gisService;

        public RestrictedZoneBuilder(IGisService gisService)
        {
            _gisService = gisService;
            _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326); // WGS 84
        }

        public async Task<IReadOnlyList<Interval<RestrictionType>>> BuildAsync(
            IReadOnlyList<Interval<RoadAccessType>> roadAccessIntervals,
            IReadOnlyList<Coordinate> geometry)
        {
            if (geometry == null || geometry.Count == 0)
                return Array.Empty<Interval<RestrictionType>>();

            var canvas = new RestrictionType?[geometry.Count];

            ApplyRoadAccessLayer(canvas, roadAccessIntervals);

            await ApplyNationalParksLayerAsync(canvas, geometry);

            return ExtractZones(canvas);
        }

        private static void ApplyRoadAccessLayer(RestrictionType?[] canvas, IReadOnlyList<Interval<RoadAccessType>> roadAccessIntervals)
        {
            if (roadAccessIntervals == null) return;

            foreach (var interval in roadAccessIntervals.Where(r => r.Value != RoadAccessType.Yes))
            {
                var restriction = MapRoadAccessToRestriction(interval.Value);

                for (int i = interval.FromIndex; i <= interval.ToIndex; i++)
                    canvas[i] = restriction;
            }
        }

        private async Task ApplyNationalParksLayerAsync(RestrictionType?[] canvas, IReadOnlyList<Coordinate> geometry)
        {
            // 1. We create a "LineString" from the route points to get the spatial envelope (Bounding Box)
            var ntsCoordinates = geometry.Select(g => new NetTopologySuite.Geometries.Coordinate(g.Longitude, g.Latitude)).ToArray();
            var routeLine = _geometryFactory.CreateLineString(ntsCoordinates);

            // 2. Ask the db for parks that overlap with the route's bounding box - this will give us a much smaller set of parks to check against
            var overlappingParks = await _gisService.GetRestrictedZonesInAreaAsync(routeLine);

            if (overlappingParks.Count == 0) return; // Trasa nejde pøes žádný park, konec.

            // 3. create local index for the overlapping parks to speed up point-in-polygon checks
            var localIndex = new STRtree<IPreparedGeometry>();
            var prepFactory = new PreparedGeometryFactory();

            foreach (var parkGeom in overlappingParks)
            {
                var preparedGeom = prepFactory.Create(parkGeom);
                localIndex.Insert(parkGeom.EnvelopeInternal, preparedGeom);
            }
            localIndex.Build();

            
            for (int i = 0; i < geometry.Count; i++)
            {
                var point = _geometryFactory.CreatePoint(ntsCoordinates[i]);

                var candidateParks = localIndex.Query(point.EnvelopeInternal);

                foreach (var parkGeom in candidateParks)
                {
                    if (parkGeom.Contains(point))
                    {
                        canvas[i] = RestrictionType.NationalPark;
                        break;
                    }
                }
            }
        }

        private static IReadOnlyList<Interval<RestrictionType>> ExtractZones(RestrictionType?[] canvas)
        {
            var zones = new List<Interval<RestrictionType>>();
            RestrictionType? current = null;
            int start = 0;

            for (int i = 0; i < canvas.Length; i++)
            {
                if (canvas[i] == current)
                    continue;

                if (current.HasValue)
                    zones.Add(new Interval<RestrictionType>(start, i - 1, current.Value));

                current = canvas[i];
                start = i;
            }

            if (current.HasValue)
                zones.Add(new Interval<RestrictionType>(start, canvas.Length - 1, current.Value));

            return zones;
        }

        private static RestrictionType MapRoadAccessToRestriction(RoadAccessType accessType)
        {
            return accessType switch
            {
                RoadAccessType.Forestry => RestrictionType.Forestry,
                RoadAccessType.Agricultural => RestrictionType.Agricultural,
                RoadAccessType.Private => RestrictionType.Private,
                RoadAccessType.Customers => RestrictionType.Customers,
                RoadAccessType.Delivery => RestrictionType.Delivery,
                RoadAccessType.Destination => RestrictionType.Destination,
                RoadAccessType.No => RestrictionType.NoAccess,
                RoadAccessType.Unknown => RestrictionType.Unknown,
                _ => throw new DomainException($"Unsupported RoadAccessType: {accessType}")
            };
        }
    }
}