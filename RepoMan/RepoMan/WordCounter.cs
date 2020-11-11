using System.Text.RegularExpressions;

namespace RepoMan
{
    public class WordCounter
    {
        private readonly Regex _counter = new Regex(
            pattern: "[\\w]+",
            options: RegexOptions.Multiline| RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        public int CountWords(string s)
            => _counter.Match(s).Length;
    }
}