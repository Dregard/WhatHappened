using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Profile.Converters
{
    public static class JsonConvertersDistributor
    {
        public static List<JsonConverter> GetAllConverters()
        {
            var result = new List<JsonConverter>
            {
                new Vector3Converter(),
                new Vector2Converter(),
                new QuaternionConverter(),
                new ColorConverter(),
            };

            return result;
        }
    }
}