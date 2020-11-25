using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using RepoMan.Analysis.Scoring;

namespace RepoMan.UnitTests
{
    public class BusinessDayScorerUnitTests
    {
        private static readonly BusinessDaysScorer _scorer = new BusinessDaysScorer();

        [Test, TestCaseSource(nameof(BusinessDayScorerTestCases))]
        public void BusinessDayScorerTests(int businessDaysOpen, double expected)
        {
            var score = _scorer.GetScore(businessDaysOpen);
            var difference = score - expected;
            Assert.IsTrue(difference < double.Epsilon);
        }
        
        private static IEnumerable<ITestCaseData> BusinessDayScorerTestCases()
        {
            yield return new TestCaseData(0, 50d)
                .SetName("Opened the same days = 50 points");
            
            yield return new TestCaseData(1, 50d)
                .SetName("Closed in 1 business day = 50 points");
            
            yield return new TestCaseData(2, 40d)
                .SetName("Closed in 2 business days = 40 points");
            
            yield return new TestCaseData(6, 6d)
                .SetName("Closed in 6 days = 0 points");
            
            yield return new TestCaseData(7, -10d)
                .SetName("Closed in 7 days = -10 points");
            
            yield return new TestCaseData(100, -940d)
                .SetName("Closed in 100 days = -940 points");
        }
    }
}