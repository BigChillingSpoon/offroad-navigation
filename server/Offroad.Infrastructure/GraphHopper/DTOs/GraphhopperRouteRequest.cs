using System.Text.Json.Serialization;
namespace Routing.Infrastructure.GraphHopper.DTOs
{
    public sealed record GraphHopperRouteRequest
    {
        [JsonPropertyName("points")]
        public required double[][] Points { get; init; }

        [JsonPropertyName("profile")]
        public required string Profile { get; init; }

        [JsonPropertyName("elevation")]
        public bool Elevation { get; init; }

        [JsonPropertyName("instructions")]
        public bool Instructions { get; init; }

        [JsonPropertyName("calc_points")]
        public bool CalcPoints { get; init; }

        [JsonPropertyName("points_encoded")]
        public bool PointsEncoded { get; init; }

        [JsonPropertyName("details")]
        public string[]? Details { get; init; }

        [JsonPropertyName("custom_model")]
        public GraphHopperCustomModel? CustomModel { get; init; }

        [JsonPropertyName("algorithm")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Algorithm { get; init; }
        [JsonPropertyName("alternative_route.max_paths")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? AlternativeRouteMaxPaths { get; init; }

        [JsonPropertyName("alternative_route.max_share_factor")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? AlternativeRouteMaxShareFactor { get; init; }

        [JsonPropertyName("alternative_route.max_weight_factor")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? AlternativeRouteMaxWeightFactor { get; init; }
        
        [JsonPropertyName("ch.disable")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? ChDisable { get; init; }
    }

    public sealed record GraphHopperCustomModel
    {
        [JsonPropertyName("priority")]
        public List<PriorityStatement> Priority { get; init; } = new();
    }

    public sealed record PriorityStatement
    {
        [JsonPropertyName("if")]
        public required string IfCondition { get; init; }

        [JsonPropertyName("multiply_by")]
        public required double MultiplyBy { get; init; }
    }
}
