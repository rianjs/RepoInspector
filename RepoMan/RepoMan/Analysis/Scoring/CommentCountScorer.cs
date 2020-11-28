using System;
using System.Linq;
using RepoMan.Repository;

namespace RepoMan.Analysis.Scoring
{
    // It only counts as a comment if the word count is greater than 10 words.
    class CommentCountScorer :
        PullRequestScorer
    {
        public const string Label = "CommentCount";
        public override string Attribute => Label;
        public override double ScoreMultiplier => 20;
        private readonly IWordCounter _wc;
        private const int _wordCountFloor = 10;

        public CommentCountScorer(IWordCounter wordCounter)
        {
            _wc = wordCounter ?? throw new ArgumentNullException(nameof(wordCounter));
        }

        public override int Count(PullRequestDetails prDetails)
        {
            var bodyCount = _wc.Count(prDetails.Body);
            var commentCount = prDetails.AllComments
                .Select(c => _wc.Count(c.Text))
                .Count(l => l >= _wordCountFloor);

            return bodyCount + commentCount;
        }
    }
}