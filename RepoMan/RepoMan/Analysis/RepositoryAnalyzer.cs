using System;
using System.Collections.Generic;
using System.Linq;
using RepoMan.Analysis.Scoring;
using RepoMan.Records;
using RepoMan.Repository;

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
        public List<MetricSnapshot> CalculateRepositoryMetricsOverTime(IList<PullRequestMetrics> prMetrics)
        {
            var now = _clock.DateTimeOffsetUtcNow();
            var snapshotsByDate = prMetrics
                .GroupBy(m => m.ClosedAt.Date)
                .Select(dateGroup =>
                {
                    var medianCommentCount = dateGroup.Select(pr => pr.CommentCount).CalculateMedian();
                    var medianWordsPerComment = dateGroup.Select(s => s.CommentWordCount).CalculateMedian();
                    var medianTimeToClosure = dateGroup.Select(s => s.OpenFor).CalculateMedian();
                    var medianBusinessDaysToClose = dateGroup.Select(s => s.BusinessDaysOpen).CalculateMedian();
                    var commentCountPopulationVariance = dateGroup.Select(s => (double) s.CommentCount).CalculatePopulationVariance();
                    var wordsPerCommentPopulationVariance = dateGroup.Select(s => (double) s.CommentWordCount).CalculatePopulationVariance();

                    var dateSnapshot = new MetricSnapshot
                    {
                        UpdatedAt = now,
                        Date = dateGroup.Key,
                        MedianSecondsToPullRequestClosure = (int) medianTimeToClosure.TotalSeconds,
                        MedianBusinessDaysToPullRequestClosure = medianBusinessDaysToClose,
                        MedianCommentCountPerPullRequest = medianCommentCount,
                        MedianWordsPerComment = medianWordsPerComment,
                        CommentCountPopulationVariance = commentCountPopulationVariance,
                        CommentWordCountVariance = wordsPerCommentPopulationVariance,
                        PullRequestMetrics = dateGroup.ToDictionary(dg => dg.Number),
                    };
                    return dateSnapshot;
                })
                .ToList();
            return snapshotsByDate;
        }
    }
}