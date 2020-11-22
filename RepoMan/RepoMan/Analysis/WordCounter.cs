using System.Text.RegularExpressions;

namespace RepoMan.Analysis
{
    public class WordCounter :
        IWordCounter
    {
        private readonly Regex _counter = new Regex(
            pattern: @"[\w]+",
            options: RegexOptions.Multiline| RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        public int CountWords(string s) =>
            string.IsNullOrWhiteSpace(s)
                ? 0
                : _counter.Matches(s).Count;
    }
}