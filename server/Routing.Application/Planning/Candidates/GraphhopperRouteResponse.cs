using System.Text.Json.Serialization;

namespace Routing.Application.Planning.Candidates
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

        [JsonPropertyName("points")]
        public string Points { get; set; } = string.Empty; // polyline
    }
}
