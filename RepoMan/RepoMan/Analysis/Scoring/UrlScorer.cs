using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RepoMan.Records;
using RepoMan.Repository;

namespace RepoMan.Analysis.Scoring
{
    class UrlScorer :
        CommentExtractorScorer
    {
        public const string Label = "UrlCount";
        public override string Attribute => Label;
        public override double ScoreMultiplier => 5;
        
        private static readonly Regex _url = new Regex(@"(https?):\/\/[^\s$.?#].[^\s]*", RegexOptions.Compiled | RegexOptions.Multiline);

        public override int Count(PullRequestDetails prDetails)
        {
            var urlCount = prDetails.AllComments
                .SelectMany(c => Extract(c.Text))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var bodyCount = Extract(prDetails.Body).Count();
            
            return urlCount + bodyCount;
        }

        public override IEnumerable<string> Extract(string s)
            => string.IsNullOrWhiteSpace(s)
                ? Enumerable.Empty<string>()
                : _url.Matches(s).Select(m => m.Value);
    }
}