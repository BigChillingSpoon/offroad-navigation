using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Routing.Infrastructure.GraphHopper
{
    public sealed record GraphHopperOptions
    {
        public const string SectionName = "GraphHopper";

        [Required, Url]
        public string BaseUrl { get; init; } = "http://localhost:8989";

        public string? ApiKey { get; }
        public bool Instructions { get; } = false;
        public bool CalcPoints { get; } = true;
        public bool PointsEncoded { get; } = true;
        public bool Elevation { get; } = true;
        public string[] RequestedDetails { get; } = { "road_class", "surface", "track_type" };
        public string Algorithm { get; } = "alternative_route";

        //specifies how many alternatives can be returned
        public int AlternativeRouteMaxPaths { get; } = 3;

        //specifies for how much % can alternative route be same as original(0.6 = 60%)
        public double AlternativeRouteMaxShareFactor { get; } = 0.9;

        //specifies how long alternative route could be from original (2 = 2x)
        public double AlternativeRouteMaxWeightFactor { get; } = 1.5;
        public bool ChDisable { get; } = true;
    }
}

