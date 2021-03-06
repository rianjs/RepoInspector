using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RepoInspector.Records;

namespace RepoInspector.Analysis.Scoring
{
    public class CodeFenceScorer :
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

        public override int Count(PullRequest prDetails)
        {
            var codeFenceCount = prDetails.FullCommentary
                .SelectMany(c => Extract(c.Text))
                .Count();
            
            return codeFenceCount;
        }

        public override IEnumerable<string> Extract(string s) =>
            string.IsNullOrWhiteSpace(s)
                ? Enumerable.Empty<string>()
                : _codeFence.Matches(s).Select(m => m.Value);
    }
}