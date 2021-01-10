using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RepoInspector.Records
{
    public class PullRequestMetrics
    {
        public int Number { get; set; }
        public DateTimeOffset OpenedAt { get; set; }
        public DateTimeOffset ClosedAt { get; set; }
        public TimeSpan OpenFor => ClosedAt - OpenedAt;
        public int BusinessDaysOpen { get; set; }
        
        [JsonConverter(typeof(TruncatingDoubleConverter))]
        public double TotalScore { get; set; }
        
        public int CommentCount { get; set; }
        public int CommentWordCount { get; set; }
        public int ApprovalCount { get; set; }
        public int MedianWordsPerComment { get; set; }
        public List<Score> Scores { get; set; }
    }
}