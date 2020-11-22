using System;
using System.Collections.Generic;
using System.Linq;

namespace RepoMan.Analysis
{
    public class RepositoryHealthAnalyzer
    {
        public RepositoryHealthSnapshot CalculateRepositoryHealthStatistics(IList<PullRequestCommentSnapshot> snapshots)
        {
            // No, I don't care that I could just iterate this once, and aggregate the things in a single iteration and then do the math at the end
            var medianCommentCount = snapshots.Select(s => s.CommentCount).CalculateMedian();
            var medianWordsPerComment = snapshots.Select(s => s.CommentWordCount).CalculateMedian();
            var medianTimeToClosure = snapshots.Select(s => s.OpenFor).CalculateMedian();
            var businessDaysOpen = snapshots
                .Select(s => CalculateBusinessDaysOpen(s.OpenedAt, s.ClosedAt))
                .ToList();
            var medianBusinessDaysToClose = businessDaysOpen.CalculateMedian();
            var commentCountStdDev = snapshots.Select(s => (double) s.CommentCount).CalculatePopulationStdDeviation();
            var wordsPerCommentStdDev = snapshots.Select(s => (double) s.CommentWordCount).CalculatePopulationStdDeviation();

            return new RepositoryHealthSnapshot
            {
                Timestamp = DateTimeOffset.UtcNow,
                PullRequestCount = snapshots.Count,
                MedianSecondsToPullRequestClosure = (int) medianTimeToClosure.TotalSeconds,
                MedianBusinessDaysToPullRequestClosure = medianBusinessDaysToClose,
                MedianCommentCountPerPullRequest = medianCommentCount,
                MedianWordsPerComment = medianWordsPerComment,
                PullRequestCommentCountStdDeviation = Math.Round(commentCountStdDev, 2),
                CommentWordCountStdDeviation = Math.Round(wordsPerCommentStdDev, 2),
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
        
        // In spite of these being comment- and PR-level measurements, they actually reflect over *repository* health. You can't tell anything about repo health
        // from one comment or PR in isolation. It's only in the aggregate that we can infer anything about health.
        // We could aggregate each of these measure lower down, but pre-aggregating would ruin the statistics, and computers are fast.
        
        // public int MedianCommentsPerPullRequest
        // public int MedianWordsPerComment
        
        // public double StdDeviationForPrCommentCount -- higher is better
        // public double StdDeviationForCommentWordCount -- higher is better
        
        // public TimeSpan MedianTimeToPrClosure
        
        // private int BusinessDaysToPrClosure -- 1-2 business days is best
    }
    
    public class RepositoryHealthSnapshot
    {
        public DateTimeOffset Timestamp { get; set; }
        public int PullRequestCount { get; set; }
        public int MedianCommentCountPerPullRequest { get; set; }
        public int MedianWordsPerComment { get; set; }
        public double PullRequestCommentCountStdDeviation { get; set; }
        public double CommentWordCountStdDeviation { get; set; }
        public int MedianSecondsToPullRequestClosure { get; set; }
        public int MedianBusinessDaysToPullRequestClosure { get; set; }
    }
}