using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Profile.Converters
{
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("x");
            writer.WriteValue(value.x);

            writer.WritePropertyName("y");
            writer.WriteValue(value.y);

            writer.WritePropertyName("z");
            writer.WriteValue(value.z);

            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var result = new Vector3();

            reader.Read();
            reader.Read();
            result.x = Convert.ToSingle(reader.Value);

            reader.Read();
            reader.Read();
            result.y = Convert.ToSingle(reader.Value);

            reader.Read();
            reader.Read();
            result.z = Convert.ToSingle(reader.Value);

            reader.Read();

            return result;
        }
    }
}