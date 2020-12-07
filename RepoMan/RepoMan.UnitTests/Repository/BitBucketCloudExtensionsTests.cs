using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using RepoMan.Repository;

namespace RepoMan.UnitTests.Repository
{
    public class BitBucketCloudExtensionsTests
    {
        [Test]
        public void GetStateFilterTests()
        {
            var actual = ItemState.Closed.GetBbCloudStateFilter();
            var expected = "(state=\"merged\" OR state=\"superseded\" OR state=\"declined\")";
            var areSame = actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(areSame);
        }

        [Test, TestCaseSource(nameof(BuildFullQueryTestCases))]
        public string BuildFullQueryTests(IEnumerable<string> args)
            => BitBucketCloudExtensions.BuildFullQuery(args, " AND ");

        public static IEnumerable<ITestCaseData> BuildFullQueryTestCases()
        {
            yield return new TestCaseData(new List<string> {"foo", null, "bar"})
                .SetName("{foo, null, bar} returns foo AND bar")
                .Returns("foo AND bar");
            
            yield return new TestCaseData(new List<string> {"foo", null})
                .SetName("{foo, null} returns foo")
                .Returns("foo");
            
            yield return new TestCaseData(new List<string> {null, "bar"})
                .SetName("{null, bar} returns bar")
                .Returns("bar");
            
            yield return new TestCaseData(new List<string>{"foo", null, "bar", null, "baz", null})
                .SetName("{foo, null, bar, null, baz, null} returns foo AND bar AND baz")
                .Returns("foo AND bar AND baz");
        }
    }
}