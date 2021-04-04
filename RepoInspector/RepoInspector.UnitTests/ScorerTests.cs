using System;
using System.Collections.Generic;
using Markdig;
using NUnit.Framework;
using RepoInspector.Analysis.Scoring;
using RepoInspector.Records;

namespace RepoInspector.UnitTests
{
    public class ScorerTests
    {
        private static readonly CodeFragmentScorer _fragmentScorer = new(new MarkdownPipelineBuilder().Build());
        private static readonly CodeFenceScorer _fenceScorer = new(new MarkdownPipelineBuilder().Build());

        [Test]
        public void BigStringTest()
        {
            const double expectedScore = 54d;
            var bigComment = new Comment
            {
                Text = CodeBlockTests.FiveMatchesFromGitHub,
            };
            
            var prDetail = new PullRequest
            {
                Comments = new List<Comment>{bigComment},
            };
            
            // Crazy github string has 5 code fences, and 2 code fragments = score of 54
            var fragmentScore = _fragmentScorer.GetScore(prDetail);
            var fenceScore = _fenceScorer.GetScore(prDetail);
            
            var codeScore = fragmentScore.Points + fenceScore.Points;
            var shouldBeZero = expectedScore - codeScore;
            Assert.IsTrue(Math.Abs(shouldBeZero) < double.Epsilon);
        }
    }
}
