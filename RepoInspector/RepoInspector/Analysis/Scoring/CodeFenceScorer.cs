using System;
using System.Collections.Generic;
using System.Linq;
using Markdig;
using Markdig.Syntax;
using RepoInspector.Records;

namespace RepoInspector.Analysis.Scoring
{
    public class CodeFenceScorer :
        CommentExtractorScorer
    {
        public const string Label = "CodeFenceCount";
        public override string Attribute => Label;
        public override double ScoreMultiplier => 10;

        private readonly MarkdownPipeline _markdownPipeline;

        public CodeFenceScorer(MarkdownPipeline markdownPipeline)
        {
            _markdownPipeline = markdownPipeline ?? throw new ArgumentNullException(nameof(markdownPipeline));
        }

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
                // Only select *closed* fenced blocks
                : Markdown.Parse(s, _markdownPipeline).OfType<FencedCodeBlock>().Where(fcb => !fcb.IsOpen).Select(fcb => fcb.Lines.ToString());
    }
}
