namespace RepoMan.Analysis.Counters.Comments
{
    /// <summary>
    /// Counts characteristics associated with comments, These outputs are typically used as inputs to the various scorers.
    /// </summary>
    internal interface ICommentCounter
    {
        int Count(string s);
    }
}