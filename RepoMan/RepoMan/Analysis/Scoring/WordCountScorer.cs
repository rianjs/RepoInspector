using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RepoMan.Repository;

namespace RepoMan.Analysis.Scoring
{
    class WordCountScorer :
        PullRequestScorer
    {
        public const string Label = "WordCount";
        public override double ScoreMultiplier => 0.1;
        public override string Attribute => Label;
        
        private readonly Regex _counter = new Regex(
            pattern: @"[\w]+",
            options: RegexOptions.Multiline| RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        public override int Count(PullRequestDetails prDetails)
            => GetWordCounts(prDetails).Sum();

        public IEnumerable<int> GetWordCounts(PullRequestDetails prDetails)
            => prDetails.AllComments.Select(c => CountWords(c.Text));

        public int CountWords(string s)
            => string.IsNullOrWhiteSpace(s)
                ? 0
                : _counter.Matches(s).Count;
    }
}