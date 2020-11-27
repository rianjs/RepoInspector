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
        public override string Attribute => "Label";
        public override double ScoreMultiplier => 20;
        private readonly WordCountScorer _wordCounter;
        private const int _wordCountFloor = 10;

        public CommentCountScorer(WordCountScorer wordCounter)
        {
            _wordCounter = wordCounter ?? throw new ArgumentNullException(nameof(wordCounter));
        }

        public override int Count(PullRequestDetails prDetails)
        {
            return prDetails.AllComments
                .Select(c => _wordCounter.CountWords(c.Text))
                .Count(l => l >= _wordCountFloor);
        }
    }
}