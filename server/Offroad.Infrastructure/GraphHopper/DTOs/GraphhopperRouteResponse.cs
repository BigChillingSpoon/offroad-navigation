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
        public List<GraphHopperAttributeInterval<string>> SurfaceIntervals { get; set; } = new();

        [JsonPropertyName("road_class")]
        public List<GraphHopperAttributeInterval<string>> RoadClassIntervals { get; set; } = new();

        [JsonPropertyName("track_type")]
        public List<GraphHopperAttributeInterval<string>> TrackTypeIntervals { get; set; } = new();

        //may be removed in the future,  at the moment this is not used in any logic, but it can be useful for debugging and for future features
        [JsonPropertyName("car_access")]
        public List<GraphHopperAttributeInterval<bool>> CarAccessIntervals { get; set; } = new();

        [JsonPropertyName("road_access")]
        public List<GraphHopperAttributeInterval<string>> RoadAccessIntervals { get; set; } = new();
        [JsonPropertyName("road_environment")]
        public List<GraphHopperAttributeInterval<string>> RoadEnvironmentIntervals { get; set; } = new();

        [JsonPropertyName("custom_barrier")]
        public List<GraphHopperAttributeInterval<string>> BarrierIntervals { get; set; } = new();
    }
}

