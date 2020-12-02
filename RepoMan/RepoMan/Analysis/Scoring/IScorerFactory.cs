namespace RepoMan.Analysis.Scoring
{
    public interface IScorerFactory
    {
        Scorer GetScorerByAttribute(string attribute);
    }
}