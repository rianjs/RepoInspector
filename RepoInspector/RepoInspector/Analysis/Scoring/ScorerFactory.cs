using System;
using System.Collections.Generic;

namespace RepoInspector.Analysis.Scoring
{
    public class ScorerFactory :
        IScorerFactory
    {
        private readonly Dictionary<string, Scorer> _scorers;

        public ScorerFactory(Dictionary<string, Scorer> scorers)
        {
            _scorers = scorers ?? throw new ArgumentNullException();
        }

        public Scorer GetScorerByAttribute(string attribute)
        {
            if (!_scorers.TryGetValue(attribute, out var scorer))
            {
                throw new ArgumentException($"{attribute} is not a recognized Scorer");
            }

            return scorer;
        }
    }
}