using System;
using RepoInspector.Records;

namespace RepoInspector.Analysis.Scoring
{
    /// <summary>
    /// Full credit is given for pull requests closed in less than 2 business days, otherwise 10 points per days is deducted for every business day the pull
    /// request remains open. So if a pull request is open for 2 business days, a score of 40 is returned. If a pull request is open for 7 business days, a
    /// score of -10 is returned.
    /// </summary>
    public class BusinessDaysScorer :
        PullRequestScorer
    {
        public const string Label = "BusinessDaysOpen";
        public override string Attribute => Label;
        public override double ScoreMultiplier => -10;
        
        public override int Count(PullRequest prDetails)
        {
            var open = prDetails.OpenedAt;
            var close = prDetails.ClosedAt;
            
            var fractionalDaysOpen = (close - open).TotalDays;
            if (fractionalDaysOpen < 1d)
            {
                return close.DayOfWeek == DayOfWeek.Saturday || close.DayOfWeek == DayOfWeek.Sunday
                    ? 0
                    : 1;
            }
            
            var bDays = (fractionalDaysOpen * 5 - (open.DayOfWeek - close.DayOfWeek) * 2) / 7;
            if (open.DayOfWeek == DayOfWeek.Saturday)
            {
                bDays--;
            }

            if (close.DayOfWeek == DayOfWeek.Sunday)
            {
                bDays--;
            }

            // There are cases where this algorithm generates a negative number.
            // One such case is:
            // - Open = 4/25/2020 5:14:22 PM +00:00 (Saturday)
            // - Close = 4/27/2020 4:27:34 PM +00:00 (Monday)
            // So just constrain it to 0
            var asInt = (int) bDays;
            return asInt <= 0
                ? 0
                : asInt;
        }

        public override Score GetScore(PullRequest prDetails)
        {
            var businessDaysOpen = Count(prDetails);
            if (businessDaysOpen < 0)
            {
                throw new ArgumentOutOfRangeException($"A pull request can't be open for a negative number of business days. Value: {businessDaysOpen}");
            }
            
            const int fullCreditDayLimit = 1;
            const int fullCredit = 50;
            var points = businessDaysOpen <= fullCreditDayLimit
                ? fullCredit
                : (businessDaysOpen - fullCreditDayLimit) * ScoreMultiplier;

            return new Score
            {
                Attribute = Attribute,
                Count = businessDaysOpen,
                Points = points,
            };
        }
    }
}