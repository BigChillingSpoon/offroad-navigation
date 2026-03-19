using System.Text.Json.Serialization;

namespace Routing.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RestrictionType
    {
        Unknown = 0,
        NationalPark,
        NatureReserve,
        Private,
        Forestry,
        Agricultural,
        Customers,
        Delivery,
        Destination,
        NoAccess
    }
}
