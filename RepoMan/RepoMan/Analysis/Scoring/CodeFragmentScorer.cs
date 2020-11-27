using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RepoMan.Repository;

namespace RepoMan.Analysis.Scoring
{
    class CodeFragmentScorer :
        CommentExtractorScorer
    {
        public const string Label = "CodeFragmentCount";
        public override string Attribute => Label;
        public override double ScoreMultiplier => 2;
        
        // This one doesn't support balanced ticks. I.e. ```foo``` is rendered the same as ``foo``. Maybe we could extend this later, but I don't have
        // time to become an expert on balancing groups in .NET regular expressions while my daughter is taking a nap.
        private static readonly Regex _codeFragment = new Regex(@"`([^\`].*?)\`", RegexOptions.Compiled | RegexOptions.Multiline);

        public override int Count(PullRequestDetails prDetails)
        {
            var codeFenceCount = prDetails.AllComments
                .SelectMany(c => Extract(c.Text))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            return codeFenceCount;
        }
        
        
        public override IEnumerable<string> Extract(string s)
            => string.IsNullOrWhiteSpace(s)
                ? Enumerable.Empty<string>()
                : _codeFragment.Matches(s).Select(m => m.Value);
    }
}