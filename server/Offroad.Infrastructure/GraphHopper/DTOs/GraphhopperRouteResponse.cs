using System.Text.Json.Serialization;
using Routing.Application.Planning.Candidates.Models;
using Routing.Infrastructure.GraphHopper.JsonConverters;
namespace Routing.Infrastructure.GraphHopper.DTOs
{
    public sealed class GraphHopperRouteResponse
    {
        [JsonPropertyName("paths")]
        public List<GraphHopperPath> Paths { get; set; } = new();

    }

    public sealed class GraphHopperPath
    {
        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("time")]
        public double TimeMs { get; set; }

        [JsonPropertyName("ascend")]
        public double Ascend { get; set; }

        [JsonPropertyName("descend")]
        public double Descend { get; set; }

        [JsonPropertyName("points")]
        public string Points { get; set; } = string.Empty; // polyline

        [JsonPropertyName("points_encoded_multiplier")]
        public double PointsEncodedMultiplier { get; set; }

        [JsonPropertyName("details")]
        public GraphHopperDetails Details { get; set; } = new();
    }

    public sealed class GraphHopperDetails
    {
        [JsonPropertyName("surface")]
        public List<GraphHopperAttributeInterval> SurfaceIntervals { get; set; } = new();

        [JsonPropertyName("road_class")]
        public List<GraphHopperAttributeInterval> RoadClassIntervals { get; set; } = new();
    }
}
