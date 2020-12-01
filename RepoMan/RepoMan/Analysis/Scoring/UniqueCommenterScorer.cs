using System.Linq;
using RepoMan.Records;

namespace RepoMan.Analysis.Scoring
{
    /// <summary>
    /// You get points for the number of unique commenters
    /// </summary>
    class UniqueCommenterScorer :
        PullRequestScorer
    {
        public const string Label = "UniqueCommenterCount";
        public override string Attribute => Label;
        public override double ScoreMultiplier => 15;
        
        public override int Count(PullRequest prDetails)
        {
            return prDetails.AllComments
                .Select(c => c.User.Id)
                .Distinct()
                .Count();
        }
    }
}