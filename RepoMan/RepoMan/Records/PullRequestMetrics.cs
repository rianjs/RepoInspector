using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RepoMan.Analysis.Scoring;
using RepoMan.Serialization;

namespace RepoMan.Records
{
    public class PullRequestMetrics :
        IEquatable<PullRequestMetrics>
    {
        public int Number { get; set; }
        public DateTimeOffset OpenedAt { get; set; }
        public DateTimeOffset ClosedAt { get; set; }
        public TimeSpan OpenFor => ClosedAt - OpenedAt;
        public int BusinessDaysOpen { get; set; }
        
        [JsonConverter(typeof(TruncatingDoubleConverter))]
        public double TotalScore { get; set; }
        
        public int CommentCount { get; set; }
        public int CommentWordCount { get; set; }
        public int ApprovalCount { get; set; }
        public int MedianWordsPerComment { get; set; }
        public List<Score> Scores { get; set; }

        public bool Equals(PullRequestMetrics other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Number == other.Number
                && OpenedAt.Equals(other.OpenedAt)
                && ClosedAt.Equals(other.ClosedAt)
                && BusinessDaysOpen == other.BusinessDaysOpen
                && TotalScore.Equals(other.TotalScore)
                && CommentCount == other.CommentCount
                && CommentWordCount == other.CommentWordCount
                && ApprovalCount == other.ApprovalCount
                && MedianWordsPerComment == other.MedianWordsPerComment
                && Equals(Scores, other.Scores);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((PullRequestMetrics) obj);
        }

        public override int GetHashCode()
        {
            var scoreHash = Scores.Aggregate(397, (current, score) => unchecked (current * 397) ^ score.GetHashCode());
            
            var hashCode = new HashCode();
            hashCode.Add(Number);
            hashCode.Add(OpenedAt);
            hashCode.Add(ClosedAt);
            hashCode.Add(BusinessDaysOpen);
            hashCode.Add(TotalScore);
            hashCode.Add(CommentCount);
            hashCode.Add(CommentWordCount);
            hashCode.Add(ApprovalCount);
            hashCode.Add(MedianWordsPerComment);
            hashCode.Add(scoreHash);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(PullRequestMetrics left, PullRequestMetrics right)
            => Equals(left, right);

        public static bool operator !=(PullRequestMetrics left, PullRequestMetrics right)
            => !Equals(left, right);
    }
}