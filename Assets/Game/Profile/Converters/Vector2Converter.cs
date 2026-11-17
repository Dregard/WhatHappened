using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Profile.Converters
{
    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("x");
            writer.WriteValue(value.x);

            writer.WritePropertyName("y");
            writer.WriteValue(value.y);

            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var result = new Vector2();

            reader.Read();
            reader.Read();
            result.x = Convert.ToSingle(reader.Value);

            reader.Read();
            reader.Read();
            result.y = Convert.ToSingle(reader.Value);

            reader.Read();
                
            return result;
        }
    }
}