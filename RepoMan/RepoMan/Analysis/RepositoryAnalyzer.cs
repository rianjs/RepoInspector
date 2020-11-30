using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RepoMan.Serialization;

namespace RepoMan.Analysis
{
    public class RepositoryAnalyzer :
        IRepositoryAnalyzer
    {
        private readonly IClock _clock;
        
        public RepositoryAnalyzer(IClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }
        // TODO: Nothing in this implementation considers anything outside of pull requests, but often commits are made directly to main
        public RepositoryMetrics CalculateRepositoryMetrics(ICollection<PullRequestMetrics> snapshots)
        {
            // No, I don't care that I could just iterate this once, and aggregate the things in a single iteration and then do the math at the end
            var medianCommentCount = snapshots.Select(s => s.CommentCount).CalculateMedian();
            var medianWordsPerComment = snapshots.Select(s => s.CommentWordCount).CalculateMedian();
            var medianTimeToClosure = snapshots.Select(s => s.OpenFor).CalculateMedian();
            var medianBusinessDaysToClose = snapshots.Select(s => s.BusinessDaysOpen).CalculateMedian();
            var commentCountPopulationVariance = snapshots.Select(s => (double) s.CommentCount).CalculatePopulationVariance();
            var wordsPerCommentPopulationVariance = snapshots.Select(s => (double) s.CommentWordCount).CalculatePopulationVariance();

            var repoMetrics = new RepositoryMetrics
            {
                Timestamp = _clock.DateTimeOffsetUtcNow(),
                PullRequestCount = snapshots.Count,
                MedianSecondsToPullRequestClosure = (int) medianTimeToClosure.TotalSeconds,
                MedianBusinessDaysToPullRequestClosure = medianBusinessDaysToClose,
                MedianCommentCountPerPullRequest = medianCommentCount,
                MedianWordsPerComment = medianWordsPerComment,
                CommentCountPopulationVariance = commentCountPopulationVariance,
                CommentWordCountVariance = wordsPerCommentPopulationVariance,
            };
            return repoMetrics;
        }
    }
    
    public class RepositoryMetrics
    {
        public DateTimeOffset Timestamp { get; set; }
        public int PullRequestCount { get; set; }
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