using System;

namespace RepoMan.Analysis.Scoring
{
    public abstract class Scorer
    {
        public abstract double ScoreMultiplier { get; }
        
        public double GetScore(int count)
        {
            const int significantDecimalPlaces = 0;
            
            var score = count * ScoreMultiplier;
            return Math.Round(score, significantDecimalPlaces, MidpointRounding.ToZero);
        }
    }
}