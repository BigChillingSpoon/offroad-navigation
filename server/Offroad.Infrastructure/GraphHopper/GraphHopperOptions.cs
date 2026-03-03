using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Routing.Infrastructure.GraphHopper
{
    public sealed record GraphHopperOptions
    {
        public const string SectionName = "GraphHopper";

        [Required, Url]
        public string BaseUrl { get; init; } = "http://localhost:8989";

        public string? ApiKey { get; init; }
        public bool Instructions { get; init; } = false;
        public bool CalcPoints { get; init; } = true;
        public bool PointsEncoded { get; init; } = true;
        public bool Elevation { get; init; } = true;
        public string[] RequestedDetails { get; init; } = { "road_class", "surface" };
        public string Algorithm { get; init; } = "alternative_route";

        //specifies how many alternatives can be returned
        public int AlternativeRouteMaxPaths { get; init; } = 3;

        //specifies for how much % can alternative route be same as original(0.6 = 60%)
        public double AlternativeRouteMaxShareFactor { get; init; } = 0.6;

        //specifies how long alternative route could be from original (2 = 2x)
        public double AlternativeRouteMaxWeightFactor { get; init; } = 1.5;
        public bool ChDisable { get; init; } = true;
    }
}

