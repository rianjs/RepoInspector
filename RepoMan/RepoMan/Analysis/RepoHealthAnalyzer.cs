using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

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
            var businessDaysOpen = snapshots
                .Select(s => CalculateBusinessDaysOpen(s.OpenedAt, s.ClosedAt))
                .ToList();
            var medianBusinessDaysToClose = businessDaysOpen.CalculateMedian();
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
        
        private int CalculateBusinessDaysOpen(DateTimeOffset open, DateTimeOffset close)
        {
            var fractionalDaysOpen = (close - open).TotalDays;
            if (fractionalDaysOpen < 1d)
            {
                return close.DayOfWeek == DayOfWeek.Saturday || close.DayOfWeek == DayOfWeek.Sunday
                    ? 0
                    : 1;
            }
            
            var bDays = (fractionalDaysOpen * 5 - (open.DayOfWeek - close.DayOfWeek) * 2) / 7;
            if (open.DayOfWeek == DayOfWeek.Saturday)
            {
                bDays--;
            }

            if (close.DayOfWeek == DayOfWeek.Sunday)
            {
                bDays--;
            }

            return (int) bDays;
        }
    }
    
    public class RepositoryHealthSnapshot
    {
        public DateTimeOffset Timestamp { get; set; }
        public int PullRequestCount { get; set; }
        public int MedianCommentCountPerPullRequest { get; set; }
        public int MedianWordsPerComment { get; set; }
        public double CommentCountPopulationVariance { get; set; }
        [JsonIgnore]
        public double CommentCountPopulationStdDeviation => Math.Round(Math.Sqrt(CommentCountPopulationVariance), 2, MidpointRounding.AwayFromZero); 
        public double CommentWordCountVariance { get; set; }
        [JsonIgnore]
        public double CommentWordCountStdDeviation => Math.Round(Math.Sqrt(CommentWordCountVariance), 2, MidpointRounding.AwayFromZero);
        public int MedianSecondsToPullRequestClosure { get; set; }
        public int MedianBusinessDaysToPullRequestClosure { get; set; }
    }
}