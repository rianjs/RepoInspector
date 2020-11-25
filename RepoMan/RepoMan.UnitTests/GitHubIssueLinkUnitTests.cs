using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using RepoMan.Analysis.Counters;

namespace RepoMan.UnitTests
{
    public class GitHubIssueLinkUnitTests
    {
        private static readonly GitHubIssueLinkCounter _issueCounter = new GitHubIssueLinkCounter();
        
        [Test, TestCaseSource(nameof(IssueCounterTestCases))]
        public int GitHubIssueLinkTests(string s)
            => _issueCounter.Count(s);

        public static IEnumerable<ITestCaseData> IssueCounterTestCases()
        {
            yield return new TestCaseData("")
                .Returns(0)
                .SetName("Empty string returns 0");
            
            yield return new TestCaseData(" ")
                .Returns(0)
                .SetName("Whitespace string returns 0");
            
            yield return new TestCaseData(null)
                .Returns(0)
                .SetName("Null string returns 0");
            
            yield return new TestCaseData("#123")
                .Returns(1)
                .SetName("#123 returns 1");
            
            yield return new TestCaseData("#123 #123")
                .Returns(1)
                .SetName("#123 #123 returns 1");
            
            yield return new TestCaseData("#123 #456")
                .Returns(2)
                .SetName("#123 #456 returns 2");
            
            yield return new TestCaseData("#123 hello #456")
                .Returns(2)
                .SetName("#123 hello #456 returns 2");
        }
    }
}