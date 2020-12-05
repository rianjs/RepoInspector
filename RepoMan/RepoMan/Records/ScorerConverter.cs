using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RepoMan.Analysis.Scoring;

namespace RepoMan.Records
{
    /// <summary>
    /// Some values cannot be represented with a double, even when you round. For example, rounding 3.7222799322868645 using the classical technical results in
    /// 3.7200000000000002, which doesn't represent the intent of the programmer. We don't ever need values beyond 2 decimal places, because humans can't reason
    /// to that degree of fidelity. So we round to 2 decimal places, and ignore everything after the second decimal place when serializing the JSON.
    ///
    /// On deserialization, the representation goes back to 3.7200000000000002, but that's an implementation detail of floating point math, not something we
    /// need to care about as humans.
    /// </summary>
    public class ScorerConverter :
        JsonConverter<Scorer>
    {
        private readonly IScorerFactory _scorerFactory;

        public ScorerConverter(IScorerFactory scorerFactory)
        {
            _scorerFactory = scorerFactory ?? throw new ArgumentNullException(nameof(scorerFactory));
        }
        
        public override bool CanWrite => false;
        public override void WriteJson(JsonWriter writer, Scorer value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override Scorer ReadJson(JsonReader reader, Type objectType, Scorer existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var scorerBlock = JObject.Load(reader);
            foreach (var element in scorerBlock)
            {
                if (string.Equals(element.Key, "attribute", StringComparison.OrdinalIgnoreCase))
                {
                    var attribute = (string)element.Value;
                    var scorer = _scorerFactory.GetScorerByAttribute(attribute);
                    return scorer;
                }
            }

            throw new ArgumentOutOfRangeException($"'{scorerBlock}' did not contain a recognized Scorer");
        }
    }
}