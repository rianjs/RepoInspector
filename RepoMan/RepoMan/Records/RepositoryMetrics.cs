using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RepoMan.Records
{
    public class RepositoryMetrics
    {
        public DateTimeOffset Timestamp { get; set; }
        
        public HashSet<int> PullRequests { get; set; }
        public int PullRequestCount => PullRequests.Count;
        
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
    }
}