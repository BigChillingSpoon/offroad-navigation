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
    public sealed class GraphHopperDetailSegmentConverter : JsonConverter<GraphHopperDetailSegment>
    {
        public override GraphHopperDetailSegment Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected array for detail segment");

            reader.Read();
            var from = reader.GetInt32();

            reader.Read();
            var to = reader.GetInt32();

            reader.Read();
            var value = reader.GetString() ?? string.Empty;

            // move to EndArray
            reader.Read();

            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException("Expected EndArray");

            return new GraphHopperDetailSegment
            (
                FromIndex: from,
                ToIndex: to,
                Value: value
            );
        }

        public override void Write(Utf8JsonWriter writer, GraphHopperDetailSegment value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.FromIndex);
            writer.WriteNumberValue(value.ToIndex);
            writer.WriteStringValue(value.Value);
            writer.WriteEndArray();
        }
    }
}
