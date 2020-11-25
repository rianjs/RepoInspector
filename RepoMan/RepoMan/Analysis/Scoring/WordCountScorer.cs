using System;

namespace RepoMan.Analysis.Scoring
{
    class WordCountScorer :
        Scorer
    {
        public override double ScoreMultiplier => 0.1;

        public new double GetScore(int wordCount)
        {
            const int significantDecimalPlaces = 1;
            
            var score = wordCount * ScoreMultiplier;
            return Math.Round(score, significantDecimalPlaces, MidpointRounding.ToZero);
        }
    }
}