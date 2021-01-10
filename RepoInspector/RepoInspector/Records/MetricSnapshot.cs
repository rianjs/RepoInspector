using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RepoInspector.Analysis.Scoring;

namespace RepoInspector.Records
{
    /// <summary>
    /// MetricSnapshots are intended to be daily summaries of pull request and commit activity that occurred on a specific date. 
    /// </summary>
    public class MetricSnapshot
    {
        public string Owner { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        
        /// <summary>
        /// The date that this metrics snapshot represents.
        /// </summary>
        public DateTimeOffset Date { get; set; }
        
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