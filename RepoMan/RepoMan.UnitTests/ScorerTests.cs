using System;
using NUnit.Framework;
using RepoMan.Analysis.Counters;
using RepoMan.Analysis.Scoring;

namespace RepoMan.UnitTests
{
    public class ScorerTests
    {
        private static readonly CodeFragmentCounter _fragmentCounter = new CodeFragmentCounter();
        private static readonly CodeFenceCounter _fenceCounter = new CodeFenceCounter();
        
        [Test]
        public void WordCountScorerTests()
        {
            var wcScorer = new WordCountScorer();
            
            Assert.IsTrue(Math.Abs(wcScorer.ScoreMultiplier - 0.1d) < double.Epsilon);
            Assert.IsTrue(Math.Abs(wcScorer.GetScore(20) - 2d) < double.Epsilon);
        }

        [Test]
        public void CodeBlockScoreTests()
        {
            var cbScorer = new CodeFenceScorer();
            Assert.IsTrue(Math.Abs(cbScorer.ScoreMultiplier - 10d) < double.Epsilon);
        }

        [Test]
        public void BigStringTest()
        {
            // Crazy github string has 5 code fences, and 2 code fragments = score of 54
            var codeFragments = _fragmentCounter.Count(CodeBlockTests.FiveMatchesFromGitHub);
            var fragmentScore = new CodeFragmentScorer().GetScore(codeFragments);
            var codeFences = _fenceCounter.Count(CodeBlockTests.FiveMatchesFromGitHub);
            var fenceScore = new CodeFenceScorer().GetScore(codeFences);

            var codeScore = fragmentScore + fenceScore;
            var shouldBeZero = 54d - codeScore;
            Assert.IsTrue(Math.Abs(shouldBeZero) < double.Epsilon);
        }
    }
}