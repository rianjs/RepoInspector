using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using RepoMan.Analysis.Counters;
using RepoMan.Analysis.Counters.Comments;

namespace RepoMan.UnitTests
{
    public class CodeBlockTests
    {
        private static readonly CodeFragmentCounter _fragmentCounter = new CodeFragmentCounter();
        private static readonly CodeFenceCounter _fenceCounter = new CodeFenceCounter();
        const string s = "```this is not a code block``` but it is a code fragment, as is `this`!";

        [Test, TestCaseSource(nameof(CodeFenceTestCases))]
        public int CodeFenceTests(string s)
        {
            return _fenceCounter.Count(s);
        }

        public static IEnumerable<ITestCaseData> CodeFenceTestCases()
        {
            yield return new TestCaseData(s)
                .Returns(0)
                .SetName("String with 0 code blocks and 2 code fragment returns 0");
            
            yield return new TestCaseData(FiveMatchesFromGitHub)
                .Returns(5)
                .SetName("Crazy github string has 5 code fences, and 2 code fragments");
            
            yield return new TestCaseData("")
                .Returns(0)
                .SetName("Zero length string returns 0");
            
            yield return new TestCaseData(null)
                .Returns(0)
                .SetName("null string returns 0");
            
            yield return new TestCaseData("     ")
                .Returns(0)
                .SetName("Whitespace string returns 0");
            
            yield return new TestCaseData("some string")
                .Returns(0)
                .SetName("simple string returns 0");
            
            // Test single ticks...
            yield return new TestCaseData("string with one ` tick")
                .Returns(0)
                .SetName("string with one ` tick returns 0");
            
            yield return new TestCaseData("`")
                .Returns(0)
                .SetName("` returns 0");
            
            yield return new TestCaseData("``")
                .Returns(0)
                .SetName("`` returns 0");
            
            yield return new TestCaseData("` `")
                .Returns(0)
                .SetName("` ` returns 0");
            
            yield return new TestCaseData("`f`")
                .Returns(0)
                .SetName("`f` returns 0");
            
            // Swap single ticks for triple ticks
            yield return new TestCaseData("string with three ``` ticks")
                .Returns(0)
                .SetName("string with three ``` tick returns 0");
            
            yield return new TestCaseData("```")
                .Returns(0)
                .SetName("``` returns 0");
            
            yield return new TestCaseData("``````")
                .Returns(0)
                .SetName("`````` returns 0");
            
            yield return new TestCaseData("``` ```")
                .Returns(0)
                .SetName("``` ``` returns 0");
            
            yield return new TestCaseData("```f```")
                .Returns(0)
                .SetName("```f``` returns 0");
        }
        
        [Test, TestCaseSource(nameof(CodeFragmentTestCases))]
        public int CodeFragmentTests(string s)
        {
            return _fragmentCounter.Count(s);
        }

        public static IEnumerable<ITestCaseData> CodeFragmentTestCases()
        {
            yield return new TestCaseData(s)
                .Returns(2)
                .SetName("String with 0 code blocks and 2 code fragments returns 2");
            
            yield return new TestCaseData(FiveMatchesFromGitHub)
                .Returns(2)
                .SetName("Crazy github string has 5 code fences, and 2 code fragments");
            
            yield return new TestCaseData("")
                .Returns(0)
                .SetName("Zero length string returns 0");
            
            yield return new TestCaseData(null)
                .Returns(0)
                .SetName("null string returns 0");
            
            yield return new TestCaseData("     ")
                .Returns(0)
                .SetName("Whitespace string returns 0");
            
            yield return new TestCaseData("some string")
                .Returns(0)
                .SetName("simple string returns 0");
            
            // Test single ticks...
            yield return new TestCaseData("string with one ` tick")
                .Returns(0)
                .SetName("string with one ` tick returns 0");
            
            yield return new TestCaseData("`")
                .Returns(0)
                .SetName("` returns 0");
            
            yield return new TestCaseData("``")
                .Returns(0)
                .SetName("`` returns 0");
            
            yield return new TestCaseData("` `")
                .Returns(1)
                .SetName("` ` returns 1");
            
            yield return new TestCaseData("`f`")
                .Returns(1)
                .SetName("`f` returns 1");
            
            // Swap single ticks for triple ticks
            yield return new TestCaseData("string with three ``` ticks")
                .Returns(0)
                .SetName("string with three ``` tick returns 0");
            
            yield return new TestCaseData("```")
                .Returns(0)
                .SetName("``` returns 0");
            
            yield return new TestCaseData("``````")
                .Returns(0)
                .SetName("`````` returns 0");
            
            yield return new TestCaseData("``` ```")
                .Returns(1)
                .SetName("``` ``` returns 1");
            
            yield return new TestCaseData("```f```")
                .Returns(1)
                .SetName("```f``` returns 1");
        }
        
        #region UglyStrings
        // There are 5 matches here, and I tested that GitHub displays them as code fences
        public const string FiveMatchesFromGitHub = @"```csharp
baz
```

``` 
hello world
```

``` 
foo
```

hello ```bar``` world

````````````baz````````````

``` csharp 
var foo = 1;
```

```CSHARP
var foo = 1;
```";
        #endregion
    }
}