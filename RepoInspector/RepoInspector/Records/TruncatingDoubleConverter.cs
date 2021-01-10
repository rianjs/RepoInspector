using System;
using Newtonsoft.Json;

namespace RepoInspector.Records
{
    /// <summary>
    /// Some values cannot be represented with a double, even when you round. For example, rounding 3.7222799322868645 using the classical technical results in
    /// 3.7200000000000002, which doesn't represent the intent of the programmer. We don't ever need values beyond 2 decimal places, because humans can't reason
    /// to that degree of fidelity. So we round to 2 decimal places, and ignore everything after the second decimal place when serializing the JSON.
    ///
    /// On deserialization, the representation goes back to 3.7200000000000002, but that's an implementation detail of floating point math, not something we
    /// need to care about as humans.
    /// </summary>
    public class TruncatingDoubleConverter : JsonConverter<double>
    {
        public override void WriteJson(JsonWriter writer, double value, JsonSerializer serializer)
        {
            var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
            writer.WriteValue($"{rounded:F2}");
        }

        public override double ReadJson(JsonReader reader, Type objectType, double existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return double.Parse((string) reader.Value);
        }
    }
}