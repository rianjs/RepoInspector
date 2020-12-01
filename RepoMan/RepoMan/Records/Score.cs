using System.Text.Json.Serialization;

namespace RepoMan.Records
{
    public class Score
    {
        public string Attribute { get; set; }
        public int Count { get; set; }
        
        [JsonConverter(typeof(TruncatingDoubleConverter))]
        public double Points { get; set; }
    }
}