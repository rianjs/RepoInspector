using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using RepoInspector.Analysis.Normalization;

namespace RepoInspector.UnitTests
{
    public class NormalizationTests
    {
        private static readonly HtmlCommentStripper _htmlCommentStripper = new HtmlCommentStripper();

        [Test, TestCaseSource(nameof(StripHtmlTestCases))]
        public string StripHtmlTest(string s)
            => _htmlCommentStripper.Normalize(s);

        public static IEnumerable<ITestCaseData> StripHtmlTestCases()
        {
            yield return new TestCaseData("<!--no--> hello world")
                .Returns(" hello world")
                .SetName("<!--no--> hello world returns  hello world");
            yield return new TestCaseData("<!--no-->hello world")
                .Returns("hello world")
                .SetName("<!--no-->hello world returns hello world");
            yield return new TestCaseData(_bigComment)
                .Returns(_expectedBigComment)
                .SetName("Big comment with a single line comment and a multiline comment removes both comments");
        }

        private const string _bigComment = @"<!--
This is
a
multiline
comment
-->

###### Motivation

Motivation is good

###### Changes
Change is good.


<!-- Single line comment -->

- [x] Foo
- [x] Bar
- [x] Baz";

        private const string _expectedBigComment = @"

###### Motivation

Motivation is good

###### Changes
Change is good.




- [x] Foo
- [x] Bar
- [x] Baz";
    }
}