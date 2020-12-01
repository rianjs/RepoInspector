using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RepoMan.Records;
using RepoMan.Repository;

namespace RepoMan.Analysis.Scoring
{
    class CodeFenceScorer :
        CommentExtractorScorer
    {
        public const string Label = "CodeFenceCount";
        public override string Attribute => Label;
        public override double ScoreMultiplier => 10;
        
        // Be very careful when messing with this -- it wasn't lifted off StackOverflow or whatever, because the SO implementations are incomplete and/or
        // wrong at the edges. I went to GH, and tried dumb things to see how they were rendered, and then replicated the business rules here, and in the unit
        // tests.
        // You can see a lot of my testing here: https://github.com/rianjs/RepoMan/issues/12
        private static readonly Regex _codeFence = new Regex(@"^```[ ]*[\w]*[ ]*\n[\s\S]*?\n```", RegexOptions.Compiled | RegexOptions.Multiline);

        public override int Count(PullRequestDetails prDetails)
        {
            var codeFenceCount = prDetails.AllComments
                .SelectMany(c => Extract(c.Text))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var bodyCount = Extract(prDetails.Body).Count();
            
            return codeFenceCount + bodyCount;
        }

        public override IEnumerable<string> Extract(string s) =>
            string.IsNullOrWhiteSpace(s)
                ? Enumerable.Empty<string>()
                : _codeFence.Matches(s).Select(m => m.Value);
    }
}