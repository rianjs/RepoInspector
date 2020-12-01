using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RepoMan.Serialization;

namespace RepoMan.Records
{
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