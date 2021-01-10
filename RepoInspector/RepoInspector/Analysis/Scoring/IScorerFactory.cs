namespace RepoInspector.Analysis.Scoring
{
    public interface IScorerFactory
    {
        Scorer GetScorerByAttribute(string attribute);
    }
}