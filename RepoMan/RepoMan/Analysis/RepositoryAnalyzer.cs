using System;
using System.Collections.Generic;
using System.Linq;
using RepoMan.Records;

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
            if (snapshots is null || snapshots.Count == 0)
            {
                return null;
            }
            
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
                PullRequests = new HashSet<int>(snapshots.Select(s => s.Number)),
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
    
}