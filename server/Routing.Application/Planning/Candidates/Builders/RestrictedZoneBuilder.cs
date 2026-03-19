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
        // Místo obyèejné kolekce budeme držet vysoce optimalizovaný vyhledávací strom!
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

        public IReadOnlyList<RestrictedZone> Build(
            IReadOnlyList<RoadAccessInterval> roadAccessIntervals,
            IReadOnlyList<Coordinate> geometry)
        {
            if (geometry == null || geometry.Count == 0)
                return Array.Empty<RestrictedZone>();

            var canvas = new RestrictionType?[geometry.Count];

            ApplyRoadAccessLayer(canvas, roadAccessIntervals);
            ApplyNationalParksLayer(canvas, geometry);

            return ExtractZones(canvas);
        }

        private static void ApplyRoadAccessLayer(RestrictionType?[] canvas, IReadOnlyList<RoadAccessInterval> roadAccessIntervals)
        {
            if (roadAccessIntervals == null) return;

            foreach (var interval in roadAccessIntervals.Where(r => r.RoadAccess != RoadAccessType.Yes))
            {
                var restriction = MapRoadAccessToRestriction(interval.RoadAccess);

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

                // 1. Zjistíme, jestli jsme vùbec v "krabici" nìjakého parku (O(1) operace)
                var candidateParks = _parksSpatialIndex.Query(point.EnvelopeInternal);

                // 2. Pøesný výpoèet udìláme jen na kandidátech (vìtšinou 0, max 1)
                foreach (var parkGeom in candidateParks)
                {
                    if (parkGeom.Contains(point))
                    {
                        canvas[i] = RestrictionType.NationalPark;
                        break; // Ušetøíme èas, pokud by se náhodou parky pøekrývaly
                    }
                }
            }
        }
        private static IReadOnlyList<RestrictedZone> ExtractZones(RestrictionType?[] canvas)
        {
            var zones = new List<RestrictedZone>();
            RestrictionType? current = null;
            int start = 0;

            for (int i = 0; i < canvas.Length; i++)
            {
                if (canvas[i] == current)
                    continue;

                if (current.HasValue)
                {
                    zones.Add(new RestrictedZone
                    {
                        RestrictionType = current.Value,
                        FromIndex = start,
                        ToIndex = i - 1
                    });
                }

                current = canvas[i];
                start = i;
            }

            if (current.HasValue)
            {
                zones.Add(new RestrictedZone
                {
                    RestrictionType = current.Value,
                    FromIndex = start,
                    ToIndex = canvas.Length - 1
                });
            }

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
