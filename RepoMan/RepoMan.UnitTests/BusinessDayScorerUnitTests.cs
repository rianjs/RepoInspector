using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using RepoMan.Analysis.Scoring;
using RepoMan.Records;
using RepoMan.Repository;

namespace RepoMan.UnitTests
{
    public class BusinessDayScorerUnitTests
    {
        private static readonly BusinessDaysScorer _scorer = new BusinessDaysScorer();

        [Test, TestCaseSource(nameof(BusinessDayScorerTestCases))]
        public void BusinessDayScorerTests(PullRequestDetails prDetails, double expected)
        {
            var points = _scorer.GetScore(prDetails).Points;
            var difference = points - expected;
            Assert.IsTrue(difference < double.Epsilon);
        }
        
        private static IEnumerable<ITestCaseData> BusinessDayScorerTestCases()
        {
            var open = new DateTimeOffset(2020, 11, 2, 8, 0, 0, TimeSpan.FromHours(-5));
            var sameDay = new PullRequestDetails
            {
                OpenedAt = open,
                ClosedAt = open + TimeSpan.FromHours(2),
            };
            yield return new TestCaseData(sameDay, 50d)
                .SetName("Opened the same days = 50 points");
            
            var nextDay = new PullRequestDetails
            {
                OpenedAt = open,
                ClosedAt = open + TimeSpan.FromHours(26),
            };
            yield return new TestCaseData(nextDay, 50d)
                .SetName("Closed in 1 business day = 50 points");
            
            var twoDays = new PullRequestDetails
            {
                OpenedAt = open,
                ClosedAt = open + TimeSpan.FromHours(50),
            };
            yield return new TestCaseData(twoDays, 40d)
                .SetName("Closed in 2 business days = 40 points");
            
            var sixDays = new PullRequestDetails
            {
                OpenedAt = open,
                ClosedAt = open + TimeSpan.FromDays(7) + TimeSpan.FromHours(2),
            };
            yield return new TestCaseData(sixDays, 0d)
                .SetName("Closed in 6 days = 0 points");
            
            var sevenDays = new PullRequestDetails
            {
                OpenedAt = open,
                ClosedAt = open + TimeSpan.FromDays(8),
            };
            yield return new TestCaseData(sevenDays, -10d)
                .SetName("Closed in 7 days = -10 points");

            var twentyWeekends = TimeSpan.FromDays(20 * 2);
            var twentyWorkWeeks = TimeSpan.FromDays(5 * 20);
            var oneHundredDays = new PullRequestDetails
            {
                OpenedAt = open,
                ClosedAt = open + twentyWeekends + twentyWorkWeeks + TimeSpan.FromHours(1),
            };
            yield return new TestCaseData(oneHundredDays, -940d)
                .SetName("Closed in 100 days = -940 points");
        }
    }
}