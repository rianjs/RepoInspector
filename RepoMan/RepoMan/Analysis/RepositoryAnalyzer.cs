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
    
    public class RepositoryMetrics :
        IEquatable<RepositoryMetrics>
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

        public bool Equals(RepositoryMetrics other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            var fastMembers = Timestamp.Equals(other.Timestamp)
                && PullRequests.Count == other.PullRequests.Count
                && MedianCommentCountPerPullRequest == other.MedianCommentCountPerPullRequest
                && MedianWordsPerComment == other.MedianWordsPerComment
                && CommentCountPopulationVariance.Equals(other.CommentCountPopulationVariance)
                && CommentWordCountVariance.Equals(other.CommentWordCountVariance)
                && MedianSecondsToPullRequestClosure == other.MedianSecondsToPullRequestClosure
                && MedianBusinessDaysToPullRequestClosure == other.MedianBusinessDaysToPullRequestClosure;

            if (!fastMembers)
            {
                return false;
            }

            var hashSetsEqual = new HashSet<int>(PullRequests);
            hashSetsEqual.ExceptWith(other.PullRequests);
            return hashSetsEqual.Count == 0;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((RepositoryMetrics) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Timestamp,
                PullRequests.Sum(),    // Should be unique enough, and an int should be big enough -- a repo with 50,000 PRs is a little over 1 billion
                MedianCommentCountPerPullRequest,
                MedianWordsPerComment,
                CommentCountPopulationVariance,
                CommentWordCountVariance,
                MedianSecondsToPullRequestClosure,
                MedianBusinessDaysToPullRequestClosure);
        }

        public static bool operator ==(RepositoryMetrics left, RepositoryMetrics right)
            => Equals(left, right);

        public static bool operator !=(RepositoryMetrics left, RepositoryMetrics right)
            => !Equals(left, right);
    }
}