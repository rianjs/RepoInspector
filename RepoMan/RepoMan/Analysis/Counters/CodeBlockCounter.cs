using System.Text.RegularExpressions;

namespace RepoMan.Analysis.Counters
{
    class CodeFragmentCounter :
        ICounter
    {
        // This one doesn't support balanced ticks. I.e. ```foo``` is rendered the same as ``foo``. Maybe we could extend this later, but I don't have
        // time to become an expert on balancing groups in .NET regular expressions while my daughter is taking a nap.
        private static readonly Regex _codeFragment = new Regex(@"`([^\`].*?)\`", RegexOptions.Compiled | RegexOptions.Multiline);
        
        public int Count(string s)
            => string.IsNullOrWhiteSpace(s)
                ? 0
                : _codeFragment.Matches(s).Count;
    }
    
    class CodeFenceCounter :
        ICounter
    {
        // Be very careful when messing with this -- it wasn't lifted off StackOverflow or whatever, because the SO implementations are incomplete and/or
        // wrong at the edges. I went to GH, and tried dumb things to see how they were rendered, and then replicated the business rules here, and in the unit
        // tests.
        // You can see a lot of my testing here: https://github.com/rianjs/RepoMan/issues/12
        private static readonly Regex _codeFence = new Regex(@"^```[ ]*[\w]*[ ]*\n[\s\S]*?\n```", RegexOptions.Compiled | RegexOptions.Multiline);
        
        public int Count(string s)
            => string.IsNullOrWhiteSpace(s)
                ? 0
                : _codeFence.Matches(s).Count;
    }
}