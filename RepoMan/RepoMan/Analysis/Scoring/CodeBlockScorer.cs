namespace RepoMan.Analysis.Scoring
{
    class CodeFenceScorer :
        Scorer
    {
        public override double ScoreMultiplier => 10;
    }

    class CodeFragmentScorer :
        Scorer
    {
        public override double ScoreMultiplier => 2;
    }
}