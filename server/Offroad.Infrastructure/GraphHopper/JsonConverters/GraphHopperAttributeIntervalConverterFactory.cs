using Routing.Infrastructure.GraphHopper.DTOs;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Routing.Infrastructure.GraphHopper.JsonConverters
{
    public class GraphHopperAttributeIntervalConverterFactory : JsonConverterFactory
    {
            public override bool CanConvert(Type typeToConvert)
            {
                if (!typeToConvert.IsGenericType)
                {
                    return false;
                }

                return typeToConvert.GetGenericTypeDefinition() == typeof(GraphHopperAttributeInterval<>);
            }

            public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                Type valueType = typeToConvert.GetGenericArguments()[0];
                JsonConverter converter = (JsonConverter)Activator.CreateInstance(typeof(GraphHopperAttributeIntervalConverter<>).MakeGenericType(valueType));

                return converter;
            }
    }
}
