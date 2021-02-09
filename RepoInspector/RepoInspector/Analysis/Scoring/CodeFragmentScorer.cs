using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using RepoInspector.Records;

namespace RepoInspector.Analysis.Scoring
{
    public class CodeFragmentScorer :
        CommentExtractorScorer
    {
        public const string Label = "CodeFragmentCount";
        public override string Attribute => Label;
        public override double ScoreMultiplier => 2;

        private readonly MarkdownPipeline _markdownPipeline;

        public CodeFragmentScorer(MarkdownPipeline markdownPipeline)
        {
            _markdownPipeline = markdownPipeline ?? throw new ArgumentNullException(nameof(markdownPipeline));
        }
        
        // This one doesn't support balanced ticks. I.e. ```foo``` is rendered the same as ``foo``. Maybe we could extend this later, but I don't have
        // time to become an expert on balancing groups in .NET regular expressions while my daughter is taking a nap.
        private static readonly Regex _codeFragment = new Regex(@"`([^\`].*?)\`", RegexOptions.Compiled | RegexOptions.Multiline);

        public override int Count(PullRequest prDetails)
        {
            var codeFenceCount = prDetails.FullCommentary
                .SelectMany(c => Extract(c.Text))
                .Count();
            
            return codeFenceCount;
        }


        public override IEnumerable<string> Extract(string s)
            => string.IsNullOrWhiteSpace(s)
                ? Enumerable.Empty<string>()
                : Markdown.Parse(s, _markdownPipeline).Descendants().OfType<CodeInline>().Select(ci => ci.Content);
    }
}
