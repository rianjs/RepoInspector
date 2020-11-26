namespace RepoMan.Analysis.Scoring.PullRequests
{
    class ApprovalScorer :
        Scorer
    {
        public override double ScoreMultiplier => 25;
    }
}