using System;
using System.Linq;
using RepoInspector.Records;

namespace RepoInspector.Analysis.Scoring
{
    public class CommentCountScorer :
        PullRequestScorer
    {
        public const string Label = "CommentCount";
        public override string Attribute => Label;
        public override double ScoreMultiplier => 20;
        private readonly IWordCounter _wc;
        
        public CommentCountScorer(){}    // For deserialization only

        public CommentCountScorer(IWordCounter wordCounter)
        {
            _wc = wordCounter ?? throw new ArgumentNullException(nameof(wordCounter));
        }

        public override int Count(PullRequest prDetails)
        {
            var titleCount = _wc.Count(prDetails.Title);
            var commentCount = prDetails.FullCommentary
                .Select(c => _wc.Count(c.Text))
                .Count();

            return titleCount + commentCount;
        }
    }
}