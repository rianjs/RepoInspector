namespace RepoMan.Analysis.Scoring
{
    /// <summary>
    /// You get points for the number of unique commenters
    /// </summary>
    class UniqueCommenterScorer :
        Scorer
    {
        public override double ScoreMultiplier => 15;
    }
}