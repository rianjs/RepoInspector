using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using RepoMan.Analysis.Scoring;
using RepoMan.Repository;

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
            
            var prDetail = new PullRequestDetails
            {
                CommitComments = new List<Comment>{bigComment},
            };
            
            // Crazy github string has 5 code fences, and 2 code fragments = score of 54
            // var codeFragments = _fragmentScorer.Count(prDetail);
            var fragmentScore = _fragmentScorer.GetScore(prDetail);
            // var codeFences = _fenceScorer.Count(CodeBlockTests.FiveMatchesFromGitHub);
            var fenceScore = new CodeFenceScorer().GetScore(prDetail);
            
            var codeScore = fragmentScore.Points + fenceScore.Points;
            var shouldBeZero = expectedScore - codeScore;
            Assert.IsTrue(Math.Abs(shouldBeZero) < double.Epsilon);
        }

        private static PullRequestDetails GetPullRequestDetails()
        {
            return new PullRequestDetails
            {
                OpenedAt = _now,
                ClosedAt = _now + TimeSpan.FromHours(1),
                Id = 1233456789,
                Number = 123,
                CommitComments = new List<Comment>(),
                IsFullyInterrogated = true,
            };
        }
    }
}