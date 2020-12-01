using System;
using System.Collections.Generic;
using NUnit.Framework;
using RepoMan.Analysis.Scoring;
using RepoMan.Records;

namespace RepoMan.UnitTests
{
    public class ScorerTests
    {
        private static readonly DateTimeOffset _now = DateTimeOffset.Now;
        private static readonly CodeFragmentScorer _fragmentScorer = new CodeFragmentScorer();
        private static readonly CodeFenceScorer _fenceScorer = new CodeFenceScorer();

        [Test]
        public void BigStringTest()
        {
            const double expectedScore = 54d;
            var bigComment = new Comment
            {
                // CreatedAt = _now,
                // Id = 987654321,
                Text = CodeBlockTests.FiveMatchesFromGitHub,
            };
            
            var prDetail = new PullRequest
            {
                CommitComments = new List<Comment>{bigComment},
            };
            
            // Crazy github string has 5 code fences, and 2 code fragments = score of 54
            var fragmentScore = _fragmentScorer.GetScore(prDetail);
            var fenceScore = new CodeFenceScorer().GetScore(prDetail);
            
            var codeScore = fragmentScore.Points + fenceScore.Points;
            var shouldBeZero = expectedScore - codeScore;
            Assert.IsTrue(Math.Abs(shouldBeZero) < double.Epsilon);
        }

        private static PullRequest GetPullRequestDetails()
        {
            return new PullRequest
            {
                OpenedAt = _now,
                ClosedAt = _now + TimeSpan.FromHours(1),
                Id = 1233456789,
                Number = 123,
                CommitComments = new List<Comment>(),
            };
        }
    }
}