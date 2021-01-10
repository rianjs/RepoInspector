using System.Text.RegularExpressions;

namespace RepoInspector.Analysis.Normalization
{
    public class HtmlCommentStripper :
        INormalizer
    {
        private static readonly Regex _match = new Regex(@"<!--(.*?)-->", RegexOptions.Compiled | RegexOptions.Singleline);
        
        /// <summary>
        /// Removes from <!-- to a closing --> in any string. Typically applied to PR Body text.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public string Normalize(string s)
            => string.IsNullOrWhiteSpace(s)
                ? null
                : _match.Replace(s, "");
    }
}