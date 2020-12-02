using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RepoMan.Analysis.Scoring;

namespace RepoMan.Records
{
    public class RepositoryMetrics
    {
        public string Owner { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public int PullRequestCount => PullRequestMetrics?.Count ?? 0;
        
        public HashSet<Scorer> Scorers { get; set; }
        
        public int MedianCommentCountPerPullRequest { get; set; }
        public int MedianWordsPerComment { get; set; }
        
        [JsonConverter(typeof(TruncatingDoubleConverter))]
        public double CommentCountPopulationVariance { get; set; }
        
        [JsonConverter(typeof(TruncatingDoubleConverter))]
        public double CommentCountPopulationStdDeviation => Math.Sqrt(CommentCountPopulationVariance); 
        
        [JsonConverter(typeof(TruncatingDoubleConverter))]
        public double CommentWordCountVariance { get; set; }
        
        [JsonConverter(typeof(TruncatingDoubleConverter))]
        public double CommentWordCountStdDeviation => Math.Sqrt(CommentWordCountVariance);
        
        public int MedianSecondsToPullRequestClosure { get; set; }
        public int MedianBusinessDaysToPullRequestClosure { get; set; }
        
        public IDictionary<int, PullRequestMetrics> PullRequestMetrics { get; set; }
    }
}