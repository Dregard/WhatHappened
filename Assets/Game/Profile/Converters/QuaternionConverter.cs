using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Profile.Converters
{
    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("x");
            writer.WriteValue(value.x);

            writer.WritePropertyName("y");
            writer.WriteValue(value.y);

            writer.WritePropertyName("z");
            writer.WriteValue(value.z);

            writer.WritePropertyName("w");
            writer.WriteValue(value.w);

            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            reader.Read();
            reader.Read();
            var x = Convert.ToSingle(reader.Value);

            reader.Read();
            reader.Read();
            var y = Convert.ToSingle(reader.Value);

            reader.Read();
            reader.Read();
            var z = Convert.ToSingle(reader.Value);

            reader.Read();
            reader.Read();
            var w = Convert.ToSingle(reader.Value);

            reader.Read();

            return new Quaternion(x, y, z, w);
        }
    }
}