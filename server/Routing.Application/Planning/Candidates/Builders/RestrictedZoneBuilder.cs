using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Index.Strtree;
using Routing.Domain.Enums;
using Routing.Domain.Exceptions;
using Routing.Domain.ValueObjects;
using Coordinate = Routing.Domain.ValueObjects.Coordinate;

namespace Routing.Application.Planning.Candidates.Builders
{
    public class RestrictedZoneBuilder : IRestrictedZoneBuilder
    {
        private readonly GeometryFactory _geometryFactory;
        private readonly STRtree<IPreparedGeometry> _parksSpatialIndex;

        public RestrictedZoneBuilder(FeatureCollection parksCollection)
        {
            _geometryFactory = new GeometryFactory();
            _parksSpatialIndex = BuildSpatialIndex(parksCollection);
        }

        private static STRtree<IPreparedGeometry> BuildSpatialIndex(FeatureCollection parksCollection)
        {
            var index = new STRtree<IPreparedGeometry>();
            var prepFactory = new PreparedGeometryFactory();

            if (parksCollection == null) return index;

            foreach (var feature in parksCollection)
            {
                var preparedGeom = prepFactory.Create(feature.Geometry);
                index.Insert(feature.Geometry.EnvelopeInternal, preparedGeom);
            }

            index.Build();
            return index;
        }

        public IReadOnlyList<Interval<RestrictionType>> Build(
            IReadOnlyList<Interval<RoadAccessType>> roadAccessIntervals,
            IReadOnlyList<Coordinate> geometry)
        {
            if (geometry == null || geometry.Count == 0)
                return Array.Empty<Interval<RestrictionType>>();

            var canvas = new RestrictionType?[geometry.Count];

            ApplyRoadAccessLayer(canvas, roadAccessIntervals);
            ApplyNationalParksLayer(canvas, geometry);

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

        private void ApplyNationalParksLayer(RestrictionType?[] canvas, IReadOnlyList<Coordinate> geometry)
        {
            if (_parksSpatialIndex.Count == 0) return;

            for (int i = 0; i < geometry.Count; i++)
            {
                var point = _geometryFactory.CreatePoint(
                    new NetTopologySuite.Geometries.Coordinate(geometry[i].Longitude, geometry[i].Latitude));

                var candidateParks = _parksSpatialIndex.Query(point.EnvelopeInternal);

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
