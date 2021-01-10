using System.Text.RegularExpressions;

namespace RepoInspector.Analysis
{
    public class WordCounter :
        IWordCounter
    {
        private readonly Regex _counter = new Regex(
            pattern: @"[\w]+",
            options: RegexOptions.Multiline| RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        public int Count(string s)
            => string.IsNullOrWhiteSpace(s)
                ? 0
                : _counter.Matches(s).Count;
    }
}