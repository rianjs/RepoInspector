using System;
using System.Collections.Generic;
using System.Threading.Tasks.Sources;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Scratch
{
    class Program
    {
        static void Main(string[] args)
        {
            var val = 3.7222799322868645d;
            var expected = 3.72d;
            var actual = Math.Round(val, 2, MidpointRounding.AwayFromZero);  // 3.7200000000000002
            var areEqual = actual == expected;    // true
            var withinEpsilon = Math.Abs(actual - expected) < double.Epsilon; // also true

            var jsonSettings = GetDebugJsonSerializerSettings();
            var serialized = JsonConvert.SerializeObject(actual, typeof(ScoreConverter), jsonSettings);
            var deserialized = JsonConvert.DeserializeObject<double>(serialized, jsonSettings);
            
            Console.WriteLine("Hello World!");
        }
        
        public class ScoreConverter : JsonConverter<double>
        {
            public override void WriteJson(JsonWriter writer, double value, JsonSerializer serializer)
            {
                writer.WriteValue($"{value:F2}");
            }

            public override double ReadJson(JsonReader reader, Type objectType, double existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var s = (string) reader.Value;
                if (string.IsNullOrWhiteSpace(s))
                {
                    throw new ArgumentNullException(nameof(reader.Value));
                    
                }
                return double.Parse(s);
            }
        }

        private static JsonSerializerSettings GetDebugJsonSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                //For demo purposes:
                DefaultValueHandling = DefaultValueHandling.Include,
                Formatting = Formatting.Indented,
                //Otherwise:
                // DefaultValueHandling = DefaultValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Converters = new List<JsonConverter> { new StringEnumConverter(), new ScoreConverter(), },
            };
        }
    }
}