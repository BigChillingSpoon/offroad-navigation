using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Routing.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TripEventType
    {
        Barrier,
        PrivateRoad,
        ForestryRoad,
        AgriculturalRoad,
        NationalPark,
        Checkpoint,
        Hazard
    }
}
