using System;
using System.Collections.Generic;
using System.Linq;

namespace RepoMan.Analysis
{
    public class RepoHealthAnalyzer : IRepositoryHealthAnalyzer
    {
        public RepositoryHealthSnapshot CalculateRepositoryHealthStatistics(ICollection<PullRequestCommentSnapshot> snapshots)
        {
            // No, I don't care that I could just iterate this once, and aggregate the things in a single iteration and then do the math at the end
            var medianCommentCount = snapshots.Select(s => s.CommentCount).CalculateMedian();
            var medianWordsPerComment = snapshots.Select(s => s.CommentWordCount).CalculateMedian();
            var medianTimeToClosure = snapshots.Select(s => s.OpenFor).CalculateMedian();
            var medianBusinessDaysToClose = snapshots.Select(s => s.BusinessDaysOpen).CalculateMedian();
            var commentCountPopulationVariance = snapshots.Select(s => (double) s.CommentCount).CalculatePopulationVariance();
            var wordsPerCommentPopulationVariance = snapshots.Select(s => (double) s.CommentWordCount).CalculatePopulationVariance();

            return new RepositoryHealthSnapshot
            {
                Timestamp = DateTimeOffset.UtcNow,
                PullRequestCount = snapshots.Count,
                MedianSecondsToPullRequestClosure = (int) medianTimeToClosure.TotalSeconds,
                MedianBusinessDaysToPullRequestClosure = medianBusinessDaysToClose,
                MedianCommentCountPerPullRequest = medianCommentCount,
                MedianWordsPerComment = medianWordsPerComment,
                CommentCountPopulationVariance = Math.Round(commentCountPopulationVariance, 2, MidpointRounding.AwayFromZero),
                CommentWordCountVariance = Math.Round(wordsPerCommentPopulationVariance, 2, MidpointRounding.AwayFromZero),
            };
        }
    }
    
    public class RepositoryHealthSnapshot
    {
        public DateTimeOffset Timestamp { get; set; }
        public int PullRequestCount { get; set; }
        public int MedianCommentCountPerPullRequest { get; set; }
        public int MedianWordsPerComment { get; set; }
        public double CommentCountPopulationVariance { get; set; }
        public double CommentCountPopulationStdDeviation => Math.Round(Math.Sqrt(CommentCountPopulationVariance), 2, MidpointRounding.AwayFromZero); 
        public double CommentWordCountVariance { get; set; }
        public double CommentWordCountStdDeviation => Math.Round(Math.Sqrt(CommentWordCountVariance), 2, MidpointRounding.AwayFromZero);
        public int MedianSecondsToPullRequestClosure { get; set; }
        public int MedianBusinessDaysToPullRequestClosure { get; set; }
    }
}