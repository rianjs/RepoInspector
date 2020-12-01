using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RepoMan.Analysis.Scoring;
using RepoMan.Repository;
using RepoMan.Serialization;

namespace RepoMan.Analysis
{
    class PullRequestAnalyzer :
        IPullRequestAnalyzer
    {
        private readonly Dictionary<string, Scorer> _scorers;

        public PullRequestAnalyzer(IEnumerable<Scorer> scorers)
        {
            _scorers = scorers?.ToDictionary(s => s.Attribute, s => s, StringComparer.Ordinal)
                ?? throw new ArgumentNullException(nameof(scorers));
        }

        public PullRequestMetrics CalculatePullRequestMetrics(PullRequestDetails prDetails)
        {
            var scores = _scorers.Values
                .Select(s => s.GetScore(prDetails))
                .ToDictionary(s => s.Attribute, StringComparer.Ordinal);

            var wcScorer = (WordCountScorer) _scorers[WordCountScorer.Label];
            var medianCommentWordCount = wcScorer.GetWordCounts(prDetails).CalculateMedian();
            var totalScore = scores.Values.Select(s => s.Points).Sum();
            
            var snapshot = new PullRequestMetrics
            {
                Number = prDetails.Number,
                OpenedAt = prDetails.OpenedAt,
                ClosedAt = prDetails.ClosedAt,
                BusinessDaysOpen = scores[BusinessDaysScorer.Label].Count,
                TotalScore = totalScore,
                ApprovalCount = scores[ApprovalScorer.Label].Count,
                CommentCount = scores[CommentCountScorer.Label].Count,
                CommentWordCount = scores[WordCountScorer.Label].Count,
                MedianWordsPerComment = medianCommentWordCount,
                Scores = scores.Values.ToList(), 
            };
            return snapshot;
        }
    }

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
