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
        public RepositoryMetrics CalculateRepositoryMetrics(IList<PullRequestMetrics> prMetrics)
        {
            // No, I don't care that I could just iterate this once, and aggregate the things in a single iteration and then do the math at the end
            var medianCommentCount = prMetrics.Select(s => s.CommentCount).CalculateMedian();
            var medianWordsPerComment = prMetrics.Select(s => s.CommentWordCount).CalculateMedian();
            var medianTimeToClosure = prMetrics.Select(s => s.OpenFor).CalculateMedian();
            var medianBusinessDaysToClose = prMetrics.Select(s => s.BusinessDaysOpen).CalculateMedian();
            var commentCountPopulationVariance = prMetrics.Select(s => (double) s.CommentCount).CalculatePopulationVariance();
            var wordsPerCommentPopulationVariance = prMetrics.Select(s => (double) s.CommentWordCount).CalculatePopulationVariance();

            var repoMetrics = new RepositoryMetrics
            {
                UpdatedAt = _clock.DateTimeOffsetUtcNow(),
                MedianSecondsToPullRequestClosure = (int) medianTimeToClosure.TotalSeconds,
                MedianBusinessDaysToPullRequestClosure = medianBusinessDaysToClose,
                MedianCommentCountPerPullRequest = medianCommentCount,
                MedianWordsPerComment = medianWordsPerComment,
                CommentCountPopulationVariance = commentCountPopulationVariance,
                CommentWordCountVariance = wordsPerCommentPopulationVariance,
                PullRequestMetrics = prMetrics.ToDictionary(m => m.Number),
            };
            return repoMetrics;
        }

        public RepositoryMetrics CalculateRepositoryMetrics(IList<PullRequestMetrics> prMetrics, IRepoManager repoManager, IEnumerable<Scorer> scorers)
        {
            var skeleton = CalculateRepositoryMetrics(prMetrics);
            skeleton.Owner = repoManager.RepoOwner;
            skeleton.Name = repoManager.RepoName;
            skeleton.Url = repoManager.RepoUrl;
            skeleton.Scorers = scorers.ToHashSet();
            return skeleton;
        }
    }
}