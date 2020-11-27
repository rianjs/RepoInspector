using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RepoMan.Analysis.ApprovalAnalyzers;
using RepoMan.Analysis.Scoring;
using RepoMan.Repository;

namespace RepoMan.Analysis
{
    class CommentAnalyzer :
        ICommentAnalyzer
    {
        private readonly Dictionary<string, Scorer> _scorers;

        public CommentAnalyzer(IApprovalAnalyzer approvalAnalyzer, IEnumerable<Scorer> scorers)
        {
            _scorers = scorers?.ToDictionary(s => s.Attribute, s => s, StringComparer.Ordinal)
                ?? throw new ArgumentNullException(nameof(scorers));
        }

        public PullRequestCommentSnapshot CalculateCommentStatistics(PullRequestDetails prDetails)
        {
            var scores = _scorers.Values
                .Select(s => s.GetScore(prDetails))
                .ToDictionary(s => s.Attribute, StringComparer.Ordinal);

            var wcScorer = (WordCountScorer) _scorers[WordCountScorer.Label];
            var medianCommentWordCount = wcScorer.GetWordCounts(prDetails).CalculateMedian();
            var totalScore = scores.Values.Select(s => s.Points).Sum();
            
            var snapshot = new PullRequestCommentSnapshot
            {
                Timestamp = DateTimeOffset.UtcNow,
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

    public class PullRequestCommentSnapshot
    {
        public int Number { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public DateTimeOffset OpenedAt { get; set; }
        public DateTimeOffset ClosedAt { get; set; }
        
        [JsonIgnore]
        public TimeSpan OpenFor => ClosedAt - OpenedAt;
        public int BusinessDaysOpen { get; set; }

        public double TotalScore { get; set; }
        public int CommentCount { get; set; }
        public int CommentWordCount { get; set; }
        public int ApprovalCount { get; set; }
        public int MedianWordsPerComment { get; set; }
        public List<Score> Scores { get; set; }
    }
}
