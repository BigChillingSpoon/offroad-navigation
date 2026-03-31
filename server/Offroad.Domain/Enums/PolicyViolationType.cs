using System.Text.Json.Serialization;

namespace Routing.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PolicyViolationType
    {
        Gates,
        PrivateRoads
    }
}
