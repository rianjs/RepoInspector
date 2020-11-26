using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace RepoMan.Analysis.Counters.Comments
{
    /// <summary>
    /// Pull requests are just issues with code. The numbers associated with them are auto-incrementing across both PRs and Issues. 
    /// </summary>
    class GitHubIssueLinkCounter :
        ICommentCounter
    {
        private static readonly Regex _issue = new Regex(@"#([\d]+)", RegexOptions.Compiled | RegexOptions.Multiline);
        
        /// <summary>
        /// Returns the unique number of issues or pull request that are linked to in the text 
        /// </summary>
        public int Count(string s)
        {
            // Text looks like this, which is turned into a URL automagically: "Closing in favor of #121 "
            if (string.IsNullOrWhiteSpace(s))
            {
                return 0;
            }

            var uniques = _issue.Matches(s)
                .Select(m => m.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            return uniques;
        }
    }
}