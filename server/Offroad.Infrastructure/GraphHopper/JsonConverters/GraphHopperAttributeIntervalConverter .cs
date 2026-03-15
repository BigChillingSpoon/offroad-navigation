using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Routing.Infrastructure.GraphHopper.DTOs;

namespace Routing.Infrastructure.GraphHopper.JsonConverters
{
    public sealed class GraphHopperAttributeIntervalConverter<T> : JsonConverter<GraphHopperAttributeInterval<T>>
    {
        public override GraphHopperAttributeInterval<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected array for detail segment");

            reader.Read();
            var from = reader.GetInt32();

            reader.Read();
            var to = reader.GetInt32();

            reader.Read();
            var value = JsonSerializer.Deserialize<T>(ref reader, options);
            // move to EndArray
            reader.Read();

            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException("Expected EndArray");

            return new GraphHopperAttributeInterval<T>
            (
                FromIndex: from,
                ToIndex: to,
                Value: value
            );
        }

        public override void Write(Utf8JsonWriter writer, GraphHopperAttributeInterval<T> interval, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            JsonSerializer.Serialize(writer, interval.FromIndex, options);
            JsonSerializer.Serialize(writer, interval.ToIndex, options);
            JsonSerializer.Serialize(writer, interval.Value, options);
            writer.WriteEndArray();
        }
    }
}
