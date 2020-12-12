using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RepoMan.Records;

namespace RepoMan.Analysis.Scoring
{
    class UrlScorer :
        CommentExtractorScorer
    {
        public const string Label = "UrlCount";
        public override string Attribute => Label;
        public override double ScoreMultiplier => 5;
        
        private static readonly Regex _url = new Regex(@"(https?):\/\/[^\s$.?#].[^\s]*", RegexOptions.Compiled | RegexOptions.Multiline);

        public override int Count(PullRequest prDetails)
        {
            var urlCount = prDetails.FullCommentary
                .SelectMany(c => Extract(c.Text))
                .Count();

            var titleCount = Extract(prDetails.Title).Count();
            return urlCount + titleCount;
        }

        public override IEnumerable<string> Extract(string s)
            => string.IsNullOrWhiteSpace(s)
                ? Enumerable.Empty<string>()
                : _url.Matches(s).Select(m => m.Value);
    }
}