using System.Text.Json.Serialization;
using RepoMan.Serialization;

namespace RepoMan.Analysis.Scoring
{
    public class Score
    {
        public string Attribute { get; set; }
        public int Count { get; set; }
        
        [JsonConverter(typeof(TruncatingDoubleConverter))]
        public double Points { get; set; }
    }
}