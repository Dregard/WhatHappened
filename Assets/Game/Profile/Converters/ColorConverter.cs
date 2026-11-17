using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Profile.Converters
{
    public class ColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            var rgba = ColorUtility.ToHtmlStringRGBA(value);
            writer.WriteValue(rgba);
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var rgba = $"#{reader.Value}";

            if (!ColorUtility.TryParseHtmlString(rgba, out var color))
            {
                color = Color.white;
            }

            return color;
        }
    }
}