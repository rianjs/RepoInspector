namespace RepoMan.Analysis.Normalization
{
    public interface INormalizer
    {
        /// <summary>
        /// Removes from <!-- to a closing --> in any string. Typically applied to PR Body text.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        string Normalize(string s);
    }
}