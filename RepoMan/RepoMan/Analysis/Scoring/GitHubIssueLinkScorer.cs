using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RepoMan.Records;

namespace RepoMan.Analysis.Scoring
{
    class GitHubIssueLinkScorer :
        CommentExtractorScorer
    {
        public const string Label = "ReferencedIssueCount";
        public override string Attribute => Label;
        public override double ScoreMultiplier => 5;
        private static readonly Regex _issue = new Regex(@"#([\d]+)", RegexOptions.Compiled | RegexOptions.Multiline);
        
        public override int Count(PullRequest prDetails)
        {
            var issueLinkCount = prDetails.FullCommentary
                .SelectMany(c => Extract(c.Text))
                .Count();

            var titleCount = Extract(prDetails.Title).Count();

            return issueLinkCount + titleCount;
        }

        /// <summary>
        /// Returns the unique number of issues or pull request that are linked to in the text 
        /// </summary>
        public override IEnumerable<string> Extract(string s)
        {
            // Text looks like this, which is turned into a URL automagically: "Closing in favor of #121 "
            return string.IsNullOrWhiteSpace(s)
                ? Enumerable.Empty<string>()
                : _issue.Matches(s).Select(m => m.Value);
        }
    }
}